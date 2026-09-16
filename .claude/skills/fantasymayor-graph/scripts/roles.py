"""The role of every non-abstract class: from its base where the base decides, from its EventReader fields and
[SystemRole] marker where it does not — and the event edges that follow from the role: reacts_to, polls or
consumes."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass

from graph_draft import Edge

CLASS_KINDS = {"other", "installer", "config", "view"}
UPDATE_LOOPS = {"IUpdatedSystem", "ILateUpdatedSystem"}
READER_TYPE = "EventReader"


@dataclass
class RoleEvidence:
    update_loop: bool
    event_readers: list       # event type ids this class holds through a readonly EventReader<TEvent> field
    role_marker: str | None
    pipeline_member: bool
    turn_phase_member: bool
    startup_step: bool
    family_member: bool


@dataclass
class RoleDecision:
    role: str
    decided_by: str     # base | marker | reader | lexical | none


def decide_roles(sources, types, draft):
    event_readers_by_class = collect_event_readers(sources, draft)
    for class_id, node in list(draft.nodes.items()):
        if not node.get("declared") or node.get("abstract") or node.get("kind") not in CLASS_KINDS:
            continue
        evidence = collect_role_evidence(class_id, event_readers_by_class, types, draft)
        decision = decide_role(evidence)
        if decision.role != "none":
            draft.add_node(class_id, kind="system", role=decision.role, decided_by=decision.decided_by)
        if evidence.event_readers:
            draft.add_node(class_id, event_readers=evidence.event_readers)
        connect_event_edges(class_id, decision, evidence.event_readers, draft)
        if evidence.role_marker and not evidence.event_readers:
            draft.warn(f"marker on {class_id} without an EventReader field @ {node.get('source_location', '')} — "
                       f"a role marker is forbidden on a class that holds no reader", "system/marker-forbidden")


def collect_event_readers(sources, draft) -> dict:
    """Every class's own readonly EventReader<TEvent> fields, resolved to the event type they read — the sole
    signal a reader's role is decided from (q10): fmgraph never reads RegisterAppStateSystem<T> as a registration,
    so the reader never comes from an injects edge."""
    readers = defaultdict(list)
    for declaration in sources.declarations:
        namespaces = sources.usings.get(declaration["file"], {""})
        for declared_field in declaration["fields"]:
            field_type = declared_field["type"]
            if field_type.name != READER_TYPE or not field_type.args:
                continue
            event_id = draft.resolve(field_type.args[0], namespaces, declared_field["source_location"])
            if event_id:
                readers[declaration["id"]].append(event_id)
    return {class_id: sorted(set(event_ids)) for class_id, event_ids in readers.items()}


def collect_role_evidence(class_id: str, event_readers_by_class: dict, types, draft) -> RoleEvidence:
    ancestors = types.ancestry.get(class_id, [])
    ancestor_names = {a.node for a in ancestors}
    hosted = {e.dst for e in draft.edges if e.rel == "hosts"}
    marked = types.markers.get(class_id)
    return RoleEvidence(
        update_loop=bool(ancestor_names & UPDATE_LOOPS),
        event_readers=event_readers_by_class.get(class_id, []),
        role_marker=marked.role if marked else None,
        pipeline_member=any(a.node == "IPrioritizedUniTaskSystem" and a.args == ("MapGenerationStep",)
                            for a in ancestors),
        turn_phase_member="TurnPhaseSubSystem" in ancestor_names,
        startup_step=any(a.node == "IUniTaskSystem" and not a.args for a in ancestors),
        family_member=any(a.node in hosted and draft.nodes.get(a.node, {}).get("abstract") for a in ancestors))


def decide_role(evidence: RoleEvidence) -> RoleDecision:
    """The first true branch wins — the order is the role decision of the cascade, word for word. An Update-loop
    class holding a reader is per_frame under the [SystemRole(PerFrame)] marker, reactive otherwise — the marker
    is never required, its absence simply means reactive (q4: SystemRoleKind carries only PerFrame now)."""
    if evidence.update_loop and evidence.event_readers:
        if evidence.role_marker == "per_frame":
            return RoleDecision("per_frame", "marker")
        return RoleDecision("reactive", "reader")
    if evidence.update_loop:
        return RoleDecision("per_frame", "base")
    if evidence.pipeline_member:
        return RoleDecision("pipeline_stage", "base")
    if evidence.turn_phase_member:
        return RoleDecision("turn_phase", "base")
    if evidence.startup_step:
        return RoleDecision("startup_step", "base")
    if evidence.family_member:
        return RoleDecision("sub_system", "lexical")
    return RoleDecision("none", "none")


def connect_event_edges(class_id: str, decision: RoleDecision, event_readers: list, draft):
    """reacts_to for a reactive system, polls for a per-frame system reading under the marker, consumes for a
    reader outside any system role or on a sub_system — a reader is allowed there too (:readers-outside-systems),
    and the edge alone marks it a consumer, without handing it a system role."""
    if decision.role == "reactive":
        rel = "reacts_to"
    elif decision.role == "per_frame" and decision.decided_by == "marker":
        rel = "polls"
    elif decision.role in ("none", "sub_system"):
        rel = "consumes"
    else:
        return
    location = draft.nodes[class_id].get("source_location", "")
    for event_id in event_readers:
        draft.add_edge(Edge(class_id, event_id, rel, "EventReader field", location))
