"""Reading the game code: one tree-sitter parse and one walk per file, and out come the declarations and the raw
facts of use — ECS calls, DI registrations, Boot's state composition, C# event subscriptions — each with its place
and owner class. Nothing is resolved here: names stay as written."""
from __future__ import annotations

from dataclasses import dataclass, field as dataclass_field
from pathlib import Path
from typing import NamedTuple

import tree_sitter_c_sharp as cs
from tree_sitter import Language, Parser

LANG = Language(cs.language())

TYPE_DECLARATIONS = {"class_declaration", "struct_declaration", "record_declaration",
                     "interface_declaration", "enum_declaration"}
COLLECTION_GENERICS = {"IReadOnlyList", "IEnumerable", "IList", "List", "ICollection", "IReadOnlyCollection"}
UNKNOWN_REGISTRATION_FORMS = {"RegisterFactory", "RegisterComponentInHierarchy", "RegisterEntryPoint",
                              "AsImplementedInterfaces"}
CONST_SPECIALS = {"int.MaxValue": 2147483647, "int.MinValue": -2147483648}


@dataclass
class TypeUse:
    """A type as written at a base, a parameter or a field."""
    name: str
    args: tuple = ()
    element: "TypeUse | None" = None
    is_collection: bool = False
    is_array: bool = False


class Invocation(NamedTuple):
    """One invocation_expression read by name: the method, its type-argument names, the receiver as written."""
    method: str
    type_args: list
    receiver: str


@dataclass
class TemplateUse:
    """The generic method a call stands in, and whether the call only relays that method's own type parameters."""
    enclosing_method: str
    enclosing_params: list
    is_template_use: bool


@dataclass
class HolderMembers:
    """What a holder class declares: its archetype members, and every Archetype member written in a form outside the
    known ones — a warning, never a silent skip."""
    archetypes: dict = dataclass_field(default_factory=dict)       # (holder, member) -> declared archetype
    unknown_forms: list = dataclass_field(default_factory=list)


@dataclass
class FileFacts:
    declarations: list = dataclass_field(default_factory=list)
    usings: set = dataclass_field(default_factory=set)
    holders: dict = dataclass_field(default_factory=dict)
    ecs_sites: list = dataclass_field(default_factory=list)
    di_sites: list = dataclass_field(default_factory=list)
    subscription_sites: list = dataclass_field(default_factory=list)
    throw_sites: list = dataclass_field(default_factory=list)
    parse_warnings: list = dataclass_field(default_factory=list)


@dataclass
class SourceFacts:
    declarations: list = dataclass_field(default_factory=list)
    usings: dict = dataclass_field(default_factory=dict)          # file -> namespaces the file sees
    holders: dict = dataclass_field(default_factory=dict)         # (holder, member) -> declared archetype
    ecs_sites: list = dataclass_field(default_factory=list)
    di_sites: list = dataclass_field(default_factory=list)
    subscription_sites: list = dataclass_field(default_factory=list)
    throw_sites: list = dataclass_field(default_factory=list)   # where the code throws: owner class and member
    parse_warnings: list = dataclass_field(default_factory=list)


def read_sources(root: Path, source_files) -> SourceFacts:
    parser = Parser(LANG)
    sources = SourceFacts()
    for path in source_files:
        rel = str(path.relative_to(root))
        try:
            file_facts = read_file(parser, root, path)
        except Exception as error:   # one unreadable file is a warning in the graph, and the scan goes on
            sources.parse_warnings.append(f"parse error in {rel}: {error}")
            continue
        sources.declarations.extend(file_facts.declarations)
        sources.usings[rel] = file_facts.usings
        sources.holders.update(file_facts.holders)
        sources.ecs_sites.extend(file_facts.ecs_sites)
        sources.di_sites.extend(file_facts.di_sites)
        sources.subscription_sites.extend(file_facts.subscription_sites)
        sources.throw_sites.extend(file_facts.throw_sites)
        sources.parse_warnings.extend(file_facts.parse_warnings)
    return sources


def read_file(parser, root: Path, path: Path) -> FileFacts:
    src = path.read_bytes()
    rel = str(path.relative_to(root))
    file_facts = FileFacts()
    usings = file_facts.usings
    usings.add("")   # the global namespace is seen from every file

    stack = [parser.parse(src).root_node]
    while stack:
        node = stack.pop()
        stack.extend(node.children)

        if node.type in TYPE_DECLARATIONS:
            file_facts.declarations.append(read_type_declaration(node, src, rel))
            if node.type == "class_declaration" and text(field(node, "name"), src).endswith("Archetypes"):
                holder_members = read_holder_members(node, src, rel)
                file_facts.holders.update(holder_members.archetypes)
                file_facts.parse_warnings.extend(holder_members.unknown_forms)
        elif node.type == "using_directive":
            usings.add(text(first_child_of_type(node, {"qualified_name", "identifier"}), src))
        elif node.type in ("namespace_declaration", "file_scoped_namespace_declaration"):
            # the enclosing namespace and every namespace above it are in scope without a using
            parts = text(field(node, "name"), src).split(".")
            usings.update(".".join(parts[:i]) for i in range(1, len(parts) + 1))
        elif node.type == "invocation_expression":
            file_facts.ecs_sites.extend(read_ecs_call(node, src, rel))
            file_facts.di_sites.extend(read_registration(node, src, rel))
        elif node.type == "object_creation_expression":
            file_facts.di_sites.extend(read_state_composition(node, src, rel))
        elif node.type == "assignment_expression":
            file_facts.subscription_sites.extend(read_subscription(node, src, rel))
        elif node.type in ("throw_statement", "throw_expression"):
            # where the code throws — the member a key-allocation site must throw in
            file_facts.throw_sites.append(site_of(node, src, rel))
        elif node.type == "element_access_expression":
            accessed = field(node, "expression")
            if accessed is not None and accessed.type == "identifier":
                file_facts.ecs_sites.append({"site": "index_usage", "field": text(accessed, src),
                                             **site_of(node, src, rel)})
    return file_facts


def read_type_declaration(declaration, src: bytes, rel: str) -> dict:
    modifiers = {text(c, src) for c in declaration.children if c.type == "modifier"}
    base_list = first_child_of_type(declaration, {"base_list"})
    body = field(declaration, "body")
    members = body.children if body is not None else []
    chain = nesting_chain(declaration, src)

    fields, events, constructors, inject_methods, construct_methods = [], [], [], [], []
    methods = []
    priority, consts = None, {}
    for member in members:
        if member.type == "field_declaration":
            fields.extend(read_fields(member, src, rel))
            consts.update(read_consts(member, src, chain))
        elif member.type in ("event_field_declaration", "event_declaration"):
            events.extend(read_event_names(member, src))
        elif member.type == "constructor_declaration":
            constructors.append(read_parameters(member, src, rel))
        elif member.type == "method_declaration":
            methods.append(text(field(member, "name"), src))
            attributes = read_attributes(member, src)
            if any(a["name"] == "Inject" for a in attributes):
                inject_methods.append(read_parameters(member, src, rel))
            if text(field(member, "name"), src) == "Construct":
                construct_methods.append(read_parameters(member, src, rel))
        elif member.type in ("operator_declaration", "conversion_operator_declaration"):
            methods.append(f"operator {text(field(member, 'operator'), src)}".strip())
        elif member.type == "property_declaration" and text(field(member, "name"), src) == "Priority":
            arrow = first_child_of_type(member, {"arrow_expression_clause"})
            if arrow is not None:
                priority = text(arrow, src).lstrip("=>").strip()

    return {
        "kind": declaration.type.replace("_declaration", ""),
        "name": text(field(declaration, "name"), src),
        "chain": chain,
        "namespace": namespace_of(declaration, src),
        "abstract": "abstract" in modifiers,
        "attributes": read_attributes(declaration, src),
        "bases": [read_type_use(c, src) for c in base_list.children if c.type not in (":", ",")] if base_list else [],
        "constructors": constructors,
        "methods": methods,
        "inject_methods": inject_methods,
        "construct_methods": construct_methods,
        "fields": fields,
        "events": events,
        "priority": priority,
        "consts": consts,
        "folder": str(Path(rel).parent) + "/",
        "file": rel,
        "source_location": f"{rel}:{line_of(declaration)}",
    }


def read_fields(member, src: bytes, rel: str) -> list[dict]:
    variables = first_child_of_type(member, {"variable_declaration"})
    if variables is None:
        return []
    type_use = read_type_use(field(variables, "type"), src)
    attribute_names = [a["name"] for a in read_attributes(member, src)]
    return [{"name": text(field(v, "name"), src), "type": type_use, "attributes": attribute_names,
             "is_const": any(text(c, src) == "const" for c in member.children if c.type == "modifier"),
             "source_location": f"{rel}:{line_of(member)}"}
            for v in variables.children if v.type == "variable_declarator"]


def read_type_use(node, src: bytes) -> TypeUse:
    """identifier / generic_name / qualified_name / nullable / array -> the type as written; a collection generic
    with one argument and an array carry their element."""
    if node is None:
        return TypeUse("")
    if node.type == "qualified_name":
        return read_type_use(field(node, "name") or node.children[-1], src)
    if node.type in ("nullable_type", "pointer_type"):
        return read_type_use(field(node, "type") or node.children[0], src)
    if node.type == "array_type":
        element = read_type_use(field(node, "type") or node.children[0], src)
        return TypeUse(element.name, element.args, element, is_array=True)
    if node.type == "generic_name":
        name = text(first_child_of_type(node, {"identifier"}), src)
        argument_nodes = type_argument_nodes(node)
        args = tuple(text(c, src) for c in argument_nodes)
        if name in COLLECTION_GENERICS and len(argument_nodes) == 1:
            return TypeUse(name, args, read_type_use(argument_nodes[0], src), is_collection=True)
        return TypeUse(name, args)
    return TypeUse(text(node, src))


def read_attributes(declaration, src: bytes) -> list[dict]:
    attributes = []
    for attribute_list in (c for c in declaration.children if c.type == "attribute_list"):
        for attribute in (a for a in attribute_list.children if a.type == "attribute"):
            name = read_type_use(field(attribute, "name"), src).name
            argument_list = first_child_of_type(attribute, {"attribute_argument_list"})
            args = [text(a, src) for a in argument_list.children if a.type == "attribute_argument"] \
                if argument_list else []
            attributes.append({"name": name, "args": args})
    return attributes


def read_consts(member, src: bytes, chain: str) -> dict:
    """Every `const` with a verbatim integer value (or int.MaxValue / int.MinValue) under its nesting path —
    SystemPriorities.RuntimeTick.Camera. Expressions such as X + 1 stay out."""
    if not any(text(c, src) == "const" for c in member.children if c.type == "modifier"):
        return {}
    consts = {}
    variables = first_child_of_type(member, {"variable_declaration"})
    for variable in (v for v in variables.children if v.type == "variable_declarator"):
        # the value stands straight under the declarator in tree-sitter-c-sharp 0.23; a grammar that wraps it in
        # equals_value_clause is read through the clause
        value_nodes = [c for c in variable.children if c.type not in ("identifier", "=", "equals_value_clause")]
        equals = first_child_of_type(variable, {"equals_value_clause"})
        if equals is not None:
            value_nodes = [c for c in equals.children if c.type != "="]
        value_text = text(value_nodes[-1], src).strip() if value_nodes else ""
        value = CONST_SPECIALS.get(value_text)
        if value is None and value_nodes and value_nodes[-1].type == "integer_literal":
            value = int(value_text)
        if value is not None:
            consts[f"{chain}.{text(field(variable, 'name'), src)}"] = value
    return consts


def read_event_names(member, src: bytes) -> list[str]:
    if member.type == "event_declaration":
        return [text(field(member, "name"), src)]
    variables = first_child_of_type(member, {"variable_declaration"})
    return [text(field(v, "name"), src) for v in variables.children if v.type == "variable_declarator"] \
        if variables else []


def read_parameters(method, src: bytes, rel: str) -> list[dict]:
    parameter_list = field(method, "parameters")
    return [{"name": text(field(p, "name"), src), "type": read_type_use(field(p, "type"), src),
             "source_location": f"{rel}:{line_of(p)}"}
            for p in (parameter_list.children if parameter_list else []) if p.type == "parameter"]


def read_holder_members(declaration, src: bytes, rel: str) -> HolderMembers:
    """A member returning Archetype: GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()) through => or the first
    return, with the method's type parameters. A member returning SingletonArchetypeDefinition: the manifest built
    with componentTypes.Add<T>() and Tags.Get in its body."""
    holder = text(field(declaration, "name"), src)
    body = field(declaration, "body")
    members = HolderMembers()
    for method in (m for m in (body.children if body else []) if m.type == "method_declaration"):
        member = text(field(method, "name"), src)
        returns = read_type_use(field(method, "returns"), src).name
        location = f"{rel}:{line_of(method)}"

        if returns == "SingletonArchetypeDefinition":
            components, tags = [], []
            for call in walk(method, "invocation_expression"):
                called = analyze_invocation(call, src)
                if called and called.method == "Add" and called.receiver == "componentTypes" \
                        and len(called.type_args) == 1:
                    components.append(called.type_args[0])
                elif called and called.method == "Get" and called.receiver in ("Tags", "EcsTags"):
                    tags.extend(called.type_args)
            if components:
                members.archetypes[(holder, member)] = {"components": components, "tags": tags, "type_params": [],
                                                        "params": member_parameters(method, src),
                                                        "singleton": True, "file": rel, "source_location": location}
            continue
        if returns != "Archetype":
            continue

        expression = body_expression(method)
        called = analyze_invocation(expression, src) if expression is not None else None
        args = arg_exprs(expression) if called and called.method == "GetArchetype" else []
        if len(args) < 2:
            members.unknown_forms.append(f"archetype member form outside the known ones: {holder}.{member} @ "
                                         f"{location} — not GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()) "
                                         f"[rule archetype/shape]")
            continue
        component_call, tag_call = analyze_invocation(args[0], src), analyze_invocation(args[1], src)
        type_parameters = field(method, "type_parameters")
        members.archetypes[(holder, member)] = {
            "components": list(component_call.type_args) if component_call else [],
            "tags": list(tag_call.type_args) if tag_call else [],
            "type_params": [text(field(p, "name"), src) for p in type_parameters.children
                            if p.type == "type_parameter"] if type_parameters else [],
            "params": member_parameters(method, src),
            "singleton": False, "file": rel, "source_location": location}
    return members


def member_parameters(method, src: bytes) -> list[str]:
    """The written type of each parameter of a holder member — how the store reaches it."""
    parameter_list = field(method, "parameters")
    return [read_type_use(field(p, "type"), src).name
            for p in (parameter_list.children if parameter_list else []) if p.type == "parameter"]


def read_ecs_call(invocation, src: bytes, rel: str) -> list[dict]:
    """Classify one call of the ECS API into its raw facts, each with its place and owner class."""
    called = analyze_invocation(invocation, src)
    if called is None:
        return []
    method, type_args, receiver = called
    site = site_of(invocation, src, rel)
    receiver_root = receiver.split(".")[0].split("[")[0]
    first_type = type_args[0] if type_args else ""
    # only base(...) can anchor; a call inside this(...) stands with the body — whether the base is the one a system
    # anchors on (UpdatedSystem, LateUpdatedSystem) is decided once the bases are resolved
    initializer = nearest_enclosing(invocation, {"constructor_initializer"})
    anchor = "base" if initializer is not None and first_child_of_type(initializer, {"base"}) is not None else "body"

    template = read_template_use(invocation, type_args, src)
    is_singleton_accessor = receiver == "Singletons" or receiver.endswith(".Singletons")
    if site["owner"].rsplit(".", 1)[-1] == "SingletonComponents" and template.is_template_use \
            and method in ("Add", "AddComponent", "GetComponent", "Has"):
        return []   # wrapper plumbing; its public call sites carry the facts

    ecs_sites = []
    if type_args and method not in ("Get", "Has"):
        ecs_sites.append({"site": "typed_call", "target_class": receiver_root or site["owner"].rsplit(".", 1)[-1],
                          "method": method, "targs": list(type_args), "anchor": anchor,
                          "enc_method": template.enclosing_method, "enc_tps": template.enclosing_params, **site})

    if method in ("AddComponent", "Set") and (method == "AddComponent" or is_singleton_accessor):
        target, confidence = first_type or (new_types_in_args(invocation, src)[:1] or [""])[0], "EXTRACTED"
        if not target:
            target, confidence = inferred_component_arg(invocation, src), "INFERRED"
        if target:
            ecs_sites.append({"site": "access", "access": "writes", "type": target, "confidence": confidence,
                              "via": "Singletons.Set" if method == "Set" else "AddComponent",
                              "singleton": method == "Set", **site})
    elif method in ("GetComponent", "HasComponent") and first_type:
        ecs_sites.append({"site": "access", "access": "reads", "type": first_type, "via": method,
                          "confidence": "EXTRACTED", "singleton": False, **site})
    elif method in ("Get", "Has") and first_type and is_singleton_accessor:
        ecs_sites.append({"site": "access", "access": "reads", "type": first_type, "via": f"Singletons.{method}",
                          "confidence": "EXTRACTED", "singleton": True, **site})
    elif method in ("RemoveComponent", "RemoveTag") and first_type:
        ecs_sites.append({"site": "access", "access": "removes", "type": first_type, "via": method,
                          "confidence": "EXTRACTED", "singleton": False, **site})
    elif method == "DeleteEntity" and not type_args and receiver_root:
        ecs_sites.append({"site": "delete_entity", **site})
    elif method in ("AllTags", "AnyTags", "WithoutAnyTags"):
        args = arg_exprs(invocation)
        tag_call = analyze_invocation(args[0], src) if args else None
        query = field(field(invocation, "function"), "expression")
        query_call = analyze_invocation(query, src) if query is not None else None
        ecs_sites.append({"site": "set", "via": method, "tags": list(tag_call.type_args) if tag_call else [],
                          "components": list(query_call.type_args) if query_call and query_call.method == "Query"
                          else [], **site})
    elif method == "AnyComponents":
        args = arg_exprs(invocation)
        types_call = analyze_invocation(args[0], src) if args else None
        if types_call and types_call.method == "Get":
            ecs_sites.append({"site": "any_components", "types": list(types_call.type_args), "anchor": anchor, **site})
    elif method == "Raise" and receiver.endswith("Events"):
        target = first_type or (new_types_in_args(invocation, src)[:1] or [""])[0]
        if target:
            ecs_sites.append({"site": "raise_event", "type": target, **site})
    elif method in ("CreateEntity", "CreateEntities"):
        ecs_sites.append({"site": "create_entity", "via": method, "receiver": receiver,
                          "argc": len(arg_exprs(invocation)), **site})
    elif method == "Add" and receiver.rsplit(".", 1)[-1] in ("Tags", "EcsTags", "tags"):
        ecs_sites.append({"site": "tags_add", "types": list(type_args), **site})
    elif receiver_root.endswith("Archetypes") and not template.is_template_use:
        ecs_sites.append({"site": "holder_call", "holder": receiver_root, "member": method,
                          "targs": list(type_args), "anchor": anchor, **site})
    return ecs_sites


def read_template_use(invocation, type_args: list, src: bytes) -> TemplateUse:
    """A call whose type arguments are the enclosing generic method's own type parameters relays them — a template
    use, never a concrete one; it only feeds the template worklist, keyed by (owner class, enclosing method)."""
    enclosing_method = nearest_enclosing(invocation, {"method_declaration", "local_function_statement"})
    type_parameters = field(enclosing_method, "type_parameters") if enclosing_method is not None else None
    enclosing_params = [text(field(p, "name"), src) for p in type_parameters.children
                        if p.type == "type_parameter"] if type_parameters else []
    is_template_use = bool(type_args) and bool(enclosing_params) and all(t in enclosing_params for t in type_args)
    return TemplateUse(text(field(enclosing_method, "name"), src), enclosing_params, is_template_use)


def read_registration(invocation, src: bytes, rel: str) -> list[dict]:
    """Register / RegisterInstance with the whole fluent statement: type arguments, As<…> targets, Lifetime,
    WithParameter(AppState.X). A registration form outside the known ones is a site of its own, for a warning."""
    called = analyze_invocation(invocation, src)
    if called is None:
        return []
    method, _, _ = called
    site = site_of(invocation, src, rel)

    if method in UNKNOWN_REGISTRATION_FORMS:
        return [{"site": "unknown_registration", "form": method, **site}]
    if method == "RegisterInstance":
        args = arg_exprs(invocation)
        identifier = text(args[0], src) if len(args) == 1 and args[0].type == "identifier" else None
        return [{"site": "register_instance", "identifier": identifier, **site}]
    if method != "Register":
        return []

    name_node = field(field(invocation, "function"), "name") or field(invocation, "function")
    type_args = [read_type_use(c, src) for c in type_argument_nodes(name_node)]
    if not type_args:
        # Register(typeof(EventReader<>), Lifetime.Transient) — one open registration for every closed
        # EventReader<TEvent> DI ever builds; not a Register<T>() call, so it carries no type argument.
        open_generic = open_generic_typeof((arg_exprs(invocation) or [None])[0], src)
        if open_generic:
            lifetime = next((text(a, src).split("Lifetime.")[-1].strip().rstrip(")")
                             for a in arg_exprs(invocation) if "Lifetime." in text(a, src)), None)
            return [{"site": "register_open_generic", "generic": open_generic, "lifetime": lifetime, **site}]
        return [{"site": "unknown_registration", "form": "Register without type arguments", **site}]

    statement = nearest_enclosing(invocation, {"expression_statement", "local_declaration_statement"}) or invocation
    as_targets, app_state = [], None
    for chained in walk(statement, "invocation_expression"):
        chained_name = field(field(chained, "function"), "name")
        chained_method = analyze_invocation(chained, src)
        if chained_method and chained_method.method == "As" and chained_name is not None:
            as_targets.extend(read_type_use(c, src) for c in type_argument_nodes(chained_name))
        elif chained_method and chained_method.method == "WithParameter":
            argument = text((arg_exprs(chained) or [None])[0], src)
            if argument.startswith("AppState."):
                app_state = argument.split(".", 1)[1]

    lifetime = next((text(a, src).split("Lifetime.")[-1].strip().rstrip(")")
                     for a in arg_exprs(invocation) if "Lifetime." in text(a, src)), None)
    return [{"site": "register", "type_args": type_args, "as_targets": as_targets, "lifetime": lifetime,
             "app_state": app_state, **site}]


def open_generic_typeof(node, src: bytes) -> str | None:
    """typeof(X<>) — an open generic marker: a generic_name whose <> carries no type argument at all, unlike a
    closed typeof(Foo<Bar>) or a plain typeof(Foo)."""
    if node is None or node.type != "typeof_expression":
        return None
    type_node = field(node, "type")
    if type_node is None or type_node.type != "generic_name":
        return None
    if type_argument_nodes(type_node):
        return None
    return text(first_child_of_type(type_node, {"identifier"}), src)


def read_state_composition(creation, src: bytes, rel: str) -> list[dict]:
    """new …State(…) inside Boot.Construct: the state and the Construct parameters its arguments name."""
    state = read_type_use(field(creation, "type"), src).name
    construct = nearest_enclosing(creation, {"method_declaration"})
    owner = nearest_enclosing(creation, {"class_declaration"})
    if not state.endswith("State") or construct is None or owner is None \
            or text(field(construct, "name"), src) != "Construct" or text(field(owner, "name"), src) != "Boot":
        return []
    return [{"site": "state_composition", "state": state,
             "identifiers": sorted({text(n, src) for n in walk(creation, "identifier")}),
             "construct_params": {p["name"]: p["type"] for p in read_parameters(construct, src, rel)},
             **site_of(creation, src, rel)}]


def read_subscription(assignment, src: bytes, rel: str) -> list[dict]:
    """a.E += H — the event name, the receiver as written, the owner class."""
    left = field(assignment, "left")
    operator = field(assignment, "operator")
    if left is None or left.type != "member_access_expression" or operator is None or text(operator, src) != "+=":
        return []
    return [{"event": text(field(left, "name"), src),
             "receiver": text(field(left, "expression"), src),
             **site_of(assignment, src, rel)}]


# ---------------------------------------------------------------- tree primitives
def text(node, src: bytes) -> str:
    return src[node.start_byte:node.end_byte].decode("utf-8", "replace") if node is not None else ""


def line_of(node) -> int:
    return node.start_point[0] + 1


def field(node, name: str):
    return node.child_by_field_name(name) if node is not None else None


def first_child_of_type(node, types):
    return next((c for c in node.children if c.type in types), None)


def type_argument_nodes(name_node) -> list:
    """The type nodes between the < and > of a generic name, or none."""
    argument_list = first_child_of_type(name_node, {"type_argument_list"}) if name_node is not None else None
    return [c for c in argument_list.children if c.type not in ("<", ">", ",")] if argument_list else []


def walk(node, node_type: str):
    stack = [node]
    while stack:
        current = stack.pop()
        if current.type == node_type:
            yield current
        stack.extend(current.children)


def nearest_enclosing(node, types):
    parent = node.parent
    while parent is not None and parent.type not in types:
        parent = parent.parent
    return parent


def namespace_of(node, src: bytes) -> str:
    enclosing = nearest_enclosing(node, {"namespace_declaration", "file_scoped_namespace_declaration"})
    return text(field(enclosing, "name"), src)


def nesting_chain(declaration, src: bytes) -> str:
    """The type name with the names of the types it is nested in: SystemPriorities.SubSystems.TerrainView."""
    chain = [text(field(declaration, "name"), src)]
    parent = nearest_enclosing(declaration, TYPE_DECLARATIONS)
    while parent is not None:
        chain.insert(0, text(field(parent, "name"), src))
        parent = nearest_enclosing(parent, TYPE_DECLARATIONS)
    return ".".join(chain)


def site_of(node, src: bytes, rel: str) -> dict:
    """Where a raw fact stands: its owner class (nesting chain and namespace), the member it stands in, its file and
    line. The member is how two facts are known to stand in one block — a payload write beside its CreateEvent."""
    owner = nearest_enclosing(node, TYPE_DECLARATIONS)
    member = nearest_enclosing(node, {"method_declaration", "local_function_statement", "constructor_declaration"})
    return {"owner": nesting_chain(owner, src) if owner is not None else "",
            "owner_namespace": namespace_of(owner, src) if owner is not None else "",
            "enclosing_member": text(field(member, "name"), src) if member is not None else "",
            "file": rel, "source_location": f"{rel}:{line_of(node)}"}


def analyze_invocation(invocation, src: bytes) -> Invocation | None:
    """The method, type-argument names and receiver text of an invocation_expression, or None."""
    if invocation is None or invocation.type != "invocation_expression":
        return None
    function = field(invocation, "function")
    if function is None:
        return None
    name_node, receiver = function, ""
    if function.type == "member_access_expression":
        name_node, receiver = field(function, "name"), text(field(function, "expression"), src)
    elif function.type not in ("generic_name", "identifier"):
        return None
    if name_node is not None and name_node.type == "generic_name":
        type_args = [read_type_use(c, src).name for c in type_argument_nodes(name_node)]
        return Invocation(text(first_child_of_type(name_node, {"identifier"}), src), type_args, receiver)
    return Invocation(text(name_node, src), [], receiver)


def arg_exprs(invocation) -> list:
    """The expression of each argument, without the argument wrapper and ref / out / in."""
    arguments = field(invocation, "arguments")
    expressions = []
    for child in (arguments.children if arguments else []):
        if child.type == "argument":
            expressions.extend(c for c in child.children if c.type not in ("ref", "out", "in"))
        elif child.type not in ("(", ")", ","):
            expressions.append(child)
    return expressions


def body_expression(method):
    """The expression of `=> expr;`, or of the first `return expr;` of a block body."""
    arrow = first_child_of_type(method, {"arrow_expression_clause"})
    if arrow is not None:
        return next((c for c in arrow.children if c.type != "=>"), None)
    body = field(method, "body")
    returned = next(walk(body, "return_statement"), None) if body is not None else None
    return next((c for c in returned.children if c.type not in ("return", ";")), None) if returned else None


def new_types_in_args(invocation, src: bytes) -> list[str]:
    return [read_type_use(field(e, "type"), src).name for e in arg_exprs(invocation)
            if e.type == "object_creation_expression"]


def inferred_component_arg(invocation, src: bytes) -> str:
    """AddComponent(mayorIdComponent) — a pre-built local. The type is inferred from the camelCase identifier only
    when it PascalCases to a …Component / …Tag / …Event name longer than the bare suffix: a generic helper's own
    `in T component` parameter PascalCases to the literal "Component", which is never a real type."""
    args = arg_exprs(invocation)
    if len(args) == 1 and args[0].type == "identifier":
        name = text(args[0], src)
        pascal = name[0].upper() + name[1:] if name else name
        if pascal.endswith(("Component", "Tag", "Event")) and pascal not in ("Component", "Tag", "Event"):
            return pascal
    return ""
