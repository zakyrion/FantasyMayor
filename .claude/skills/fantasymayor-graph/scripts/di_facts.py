"""The DI half of the graph: VContainer registrations with Lifetime, installer and AppState, injections from the
constructors of registered types and from [Inject] methods, the game states each registered system runs in, and the
hosts that collect a family of abstract classes."""
from __future__ import annotations

from collections import defaultdict

from graph_draft import Edge
from source_reading import APP_STATE_CONTRACT

PRIMITIVES = {"int", "uint", "float", "double", "long", "ulong", "short", "byte",
              "bool", "char", "string", "object", "void", "decimal"}


def reconcile_di_facts(sources, draft):
    connect_registrations(sources, draft)
    connect_injections(sources, draft)
    connect_app_states(sources, draft)
    connect_state_requests(sources, draft)


def connect_registrations(sources, draft):
    lifetimes, installers = defaultdict(set), defaultdict(set)
    for site in sources.di_sites:
        installer, location = site["owner"], site["source_location"]
        if not installer or draft.nodes[installer].get("kind") != "installer":
            continue

        if site["site"] == "unknown_registration":
            draft.warn(f"registration form outside the known ones: {site['form']} in {installer} @ {location}")
        elif site["site"] == "register_open_generic":
            # Register(typeof(EventReader<>), Lifetime.Transient) — one open registration serving every closed
            # EventReader<TEvent> DI ever builds. A reader's role never comes from an injects edge — it comes from
            # the EventReader field itself (roles.py).
            if not site["generic"]:
                continue
            via = f"Register({site['lifetime']})" if site["lifetime"] else "Register"
            draft.add_edge(Edge(installer, site["generic"], "registers", via, location, args=("<>",)))
            lifetimes[site["generic"]].add(site["lifetime"] or "")
            installers[site["generic"]].add(installer)
        elif site["site"] == "register_instance":
            if site["identifier"] is None:
                draft.warn(f"RegisterInstance at {location} ({installer}) — instance type is not a plain identifier, "
                           f"so its service cannot be resolved deterministically")
                continue
            identifier = site["identifier"]
            instance = draft.resolve(identifier[0].upper() + identifier[1:],
                                     sources.usings.get(site["file"], {""}), location)
            draft.add_edge(Edge(installer, instance, "registers", "RegisterInstance", location))
            installers[instance].add(installer)
        elif site["site"] == "register":
            # Register<Impl> or Register<Contract, Impl>; every As<…> target is a contract too, the impl itself excepted
            type_args = site["type_args"]
            implementation, contracts = (type_args[0], []) if len(type_args) == 1 else (type_args[1], [type_args[0]])
            contracts += [t for t in site["as_targets"] if (t.name, t.args) not in {(c.name, c.args) for c in contracts}]
            via = f"{site['form']}({site['lifetime']})" if site["lifetime"] else site["form"]
            draft.add_edge(Edge(installer, implementation.name, "registers", via, location, args=implementation.args,
                                app_state=site["app_state"]))
            for contract in contracts:
                if (contract.name, contract.args) == (implementation.name, implementation.args) or not contract.name:
                    continue
                draft.add_edge(Edge(implementation.name, contract.name, "exposes", "As", location, args=contract.args))
                draft.add_node(contract.name, contract=True)
            if implementation.name:
                lifetimes[implementation.name].add(site["lifetime"] or "")
                installers[implementation.name].add(installer)

    for service, service_installers in installers.items():
        draft.add_node(service, installer=", ".join(sorted(service_installers)),
                       lifetime=", ".join(sorted(v for v in lifetimes.get(service, ()) if v)) or None)


def connect_injections(sources, draft):
    registered = {e.dst for e in draft.edges if e.rel == "registers"}
    supplied = supplied_parameters(sources)
    for declaration in (d for d in sources.declarations if d["kind"] == "class"):
        namespaces = sources.usings.get(declaration["file"], {""})
        supplied_types, supplied_names = supplied.get(declaration["id"], (set(), set()))
        # VContainer builds a registered type through its constructor; an [Inject] method is called on any instance
        parameter_lists = [("ctor", p) for p in declaration["constructors"]] if declaration["id"] in registered else []
        parameter_lists += [("[Inject]", p) for p in declaration["inject_methods"]]
        for via, parameters in parameter_lists:
            for parameter in parameters:
                carried = parameter["type"].element or parameter["type"]
                if carried.name in PRIMITIVES:
                    continue
                if via == "ctor" and (carried.name in supplied_types or parameter["name"] in supplied_names):
                    continue   # WithParameter hands it over at registration — the container never resolves it
                dependency = draft.resolve(carried.name, namespaces, parameter["source_location"])
                is_collection = parameter["type"].element is not None
                draft.add_edge(Edge(declaration["id"], dependency, "injects", via, parameter["source_location"],
                                    args=carried.args, collection=is_collection))
                if is_collection and dependency:
                    draft.add_node(dependency, contract=True)


def supplied_parameters(sources) -> dict:
    """Per implementation: the constructor parameter types and names that EVERY registration of it supplies through
    WithParameter — RegisterAppStateSystem supplies the AppState. A parameter one registration leaves to the
    container is an injection there, so only what all registrations supply is kept out."""
    per_registration = defaultdict(list)
    for site in (s for s in sources.di_sites if s["site"] == "register"):
        implementation = site["type_args"][0] if len(site["type_args"]) == 1 else site["type_args"][1]
        per_registration[implementation.name].append((set(site["supplied_types"]), set(site["supplied_names"])))
    return {name: (set.intersection(*(types for types, _ in sets)), set.intersection(*(names for _, names in sets)))
            for name, sets in per_registration.items()}


def connect_app_states(sources, draft):
    """A game state is a class that names its Mode (Mode => AppState.X) and takes every IAppStateSystem in its
    constructor; it keeps the systems whose AppState flags contain X (AppStateSystems.Filter). So every
    registration exposed As IAppStateSystem runs in each state whose Mode its flags name — one runs_in edge per
    registration, with the registration's place and its closed generic arguments."""
    states = {}
    for declaration in (d for d in sources.declarations if d["kind"] == "class" and d["mode"]):
        takes_all_systems = any(p["type"].element is not None and p["type"].element.name == APP_STATE_CONTRACT
                                for parameters in declaration["constructors"] for p in parameters)
        if takes_all_systems:
            states.setdefault(declaration["mode"], []).append(declaration["id"])
            draft.add_node(declaration["id"], state=True)
    for mode, mode_states in sorted(states.items()):
        if len(mode_states) > 1:
            draft.warn(f"AppState.{mode} is the Mode of {len(mode_states)} game states [{', '.join(sorted(mode_states))}] — "
                       f"a state runs exactly one flag, and a flag has one state", "wiring/state-by-mode")

    exposed = {(e.src, e.source_location) for e in draft.edges if e.rel == "exposes" and e.dst == APP_STATE_CONTRACT}
    registrations = [e for e in list(draft.edges) if e.rel == "registers" and (e.dst, e.source_location) in exposed]
    places = defaultdict(list)
    for edge in registrations:
        places[(edge.dst, edge.args)].append(edge.source_location)
    for (system, args), locations in sorted(places.items()):
        if len(locations) > 1:
            draft.warn(f"{system}{'<' + ', '.join(args) + '>' if args else ''} is registered as {APP_STATE_CONTRACT} "
                       f"{len(locations)} times [{', '.join(sorted(locations))}] — a system of several states carries "
                       f"several flags in one registration", "wiring/state-by-mode")

    for edge in registrations:
        if not edge.app_state:
            draft.warn(f"{edge.dst} is exposed As {APP_STATE_CONTRACT} with no AppState @ {edge.source_location} — "
                       f"it runs in no game state", "wiring/app-state-registration")
            continue
        for flag in edge.app_state.split("|"):
            if flag not in states:
                draft.warn(f"{edge.dst} is flagged AppState.{flag} @ {edge.source_location}, but no game state "
                           f"runs Mode => AppState.{flag}", "wiring/app-state-registration")
            for state in states.get(flag, []):
                draft.add_edge(Edge(edge.dst, state, "runs_in", f"AppState.{flag}", edge.source_location,
                                    args=edge.args))


def connect_state_requests(sources, draft):
    """Where the game goes next: every literal AppState.X asked for by value becomes a requests_state edge from the
    class that asks to the game state whose Mode is X — via Events.Raise (the request Boot reads), Switch (Boot
    entering a mode itself) or assign (a mode kept in a field, never raised). A game state also keeps how it runs
    its entry systems: entry_run = parallel (RunAllAsync) or sequential (RunEntryAsync). An installer that hands the
    builder on (new X().Install(builder)) gets an installs edge at the place of the call — the order registrations
    are made in."""
    state_of_mode = {d["mode"]: d["id"] for d in sources.declarations
                     if d["kind"] == "class" and d["mode"] and draft.nodes[d["id"]].get("state")}
    for site in sources.app_state_sites:
        owner = owner_id(site, draft)
        if owner is None:
            continue
        if site["site"] == "entry_run":
            if draft.nodes[owner].get("state"):
                draft.add_node(owner, entry_run=site["entry"])
            continue
        if site["site"] == "install":
            namespaces = sources.usings.get(site["file"], {""})
            installed = draft.resolve(site["installer"], namespaces, site["source_location"])
            if installed is not None:
                draft.add_edge(Edge(owner, installed, "installs", "Install", site["source_location"]))
            continue
        state = state_of_mode.get(site["mode"])
        if state is None:
            draft.warn(f"{owner} asks for AppState.{site['mode']} via {site['via']} @ {site['source_location']}, but "
                       f"no game state runs Mode => AppState.{site['mode']}")
            continue
        if state != owner:
            draft.add_edge(Edge(owner, state, "requests_state", site["via"], site["source_location"],
                                member=site["enclosing_member"]))


def owner_id(site, draft) -> str | None:
    """The node of the class a site stands in — its nesting chain, or namespace + chain when the chain is shared."""
    for candidate in (site["owner"], f"{site['owner_namespace']}.{site['owner']}"):
        if candidate in draft.nodes:
            return candidate
    return None


def connect_hosts(sources, draft):
    """An orchestrator hosts a family two ways: every subsystem names it in OrchestratorType (the family is the class
    that declares the property — an abstract base, or the concrete itself), and — the older DI form — its
    constructor takes a collection of an abstract class."""
    for declaration in (d for d in sources.declarations if d["kind"] == "class" and d["orchestrator_type"]):
        namespaces = sources.usings.get(declaration["file"], {""})
        orchestrator = draft.resolve(declaration["orchestrator_type"], namespaces, declaration["source_location"])
        if orchestrator is None:
            draft.warn(f"{declaration['id']} names OrchestratorType {declaration['orchestrator_type']}, which resolves "
                       f"to no declaration @ {declaration['source_location']}")
            continue
        draft.add_edge(Edge(orchestrator, declaration["id"], "hosts", "OrchestratorType", declaration["source_location"]))
    for declaration in (d for d in sources.declarations if d["kind"] == "class"):
        namespaces = sources.usings.get(declaration["file"], {""})
        for parameter in (p for parameters in declaration["constructors"] for p in parameters):
            element = parameter["type"].element
            if element is None:
                continue
            family = draft.resolve(element.name, namespaces, parameter["source_location"])
            if family is not None and draft.nodes[family].get("abstract"):   # an interface element is no family
                draft.add_edge(Edge(declaration["id"], family, "hosts", "ctor", parameter["source_location"]))
