"""The DI half of the graph: VContainer registrations with Lifetime, installer and AppState, injections from the
constructors of registered types and from [Inject] methods, Boot's composition of the game states, and the hosts
that collect a family of abstract classes."""
from __future__ import annotations

from collections import defaultdict

from graph_draft import Edge

PRIMITIVES = {"int", "uint", "float", "double", "long", "ulong", "short", "byte",
              "bool", "char", "string", "object", "void", "decimal"}


def reconcile_di_facts(sources, draft):
    connect_registrations(sources, draft)
    connect_injections(sources, draft)
    connect_boot_states(sources, draft)


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
            # EventReader<TEvent> DI ever builds. RegisterAppStateSystem<T> (a project extension method) is not
            # read as a registration at all today, so a reader's role never comes from an injects edge — it
            # comes from the EventReader field itself (roles.py).
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
            via = f"Register({site['lifetime']})" if site["lifetime"] else "Register"
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
    for declaration in (d for d in sources.declarations if d["kind"] == "class"):
        namespaces = sources.usings.get(declaration["file"], {""})
        # VContainer builds a registered type through its constructor; an [Inject] method is called on any instance
        parameter_lists = [("ctor", p) for p in declaration["constructors"]] if declaration["id"] in registered else []
        parameter_lists += [("[Inject]", p) for p in declaration["inject_methods"]]
        for via, parameters in parameter_lists:
            for parameter in parameters:
                carried = parameter["type"].element or parameter["type"]
                if carried.name in PRIMITIVES:
                    continue
                dependency = draft.resolve(carried.name, namespaces, parameter["source_location"])
                is_collection = parameter["type"].element is not None
                draft.add_edge(Edge(declaration["id"], dependency, "injects", via, parameter["source_location"],
                                    args=carried.args, collection=is_collection))
                if is_collection and dependency:
                    draft.add_node(dependency, contract=True)


def connect_boot_states(sources, draft):
    for site in (s for s in sources.di_sites if s["site"] == "state_composition" and s["state"]):
        draft.add_node(site["state"], state=True)
        for identifier in site["identifiers"]:
            parameter = site["construct_params"].get(identifier)
            if parameter is None:
                continue
            carried = parameter.element or parameter
            if carried.name == "EntityStorages":
                continue   # the storage registry is an argument of the state, not a system running in it
            draft.add_edge(Edge(carried.name, site["state"], "runs_in",
                                "Boot(collection)" if parameter.element else "Boot", site["source_location"],
                                args=carried.args))


def connect_hosts(sources, draft):
    for declaration in (d for d in sources.declarations if d["kind"] == "class"):
        namespaces = sources.usings.get(declaration["file"], {""})
        for parameter in (p for parameters in declaration["constructors"] for p in parameters):
            element = parameter["type"].element
            if element is None:
                continue
            family = draft.resolve(element.name, namespaces, parameter["source_location"])
            if family is not None and draft.nodes[family].get("abstract"):   # an interface element is no family
                draft.add_edge(Edge(declaration["id"], family, "hosts", "ctor", parameter["source_location"]))
