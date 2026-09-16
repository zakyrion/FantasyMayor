"""The role of every non-abstract class: from its base where the base decides, from its [SystemRole] marker where it
does not — and the event edges that follow from the role: reacts_to or polls."""
from __future__ import annotations

from dataclasses import dataclass

from ecs_facts import EVENT_TAG
from graph_draft import Edge

CLASS_KINDS = {"other", "installer", "config", "view"}
UPDATE_LOOPS = {"IUpdatedSystem", "ILateUpdatedSystem"}
INT_MAX = 2147483647


@dataclass
class RoleEvidence:
    update_loop: bool
    sweeps_events: bool
    anchored_on_event: bool
    table_anchored: bool
    holds_event_archetype: bool
    role_marker: str | None
    pipeline_member: bool
    turn_phase_member: bool
    startup_step: bool
    family_member: bool


@dataclass
class RoleDecision:
    role: str
    decided_by: str     # base | marker | lexical | none


def decide_roles(types, draft):
    for class_id, node in list(draft.nodes.items()):
        if not node.get("declared") or node.get("abstract") or node.get("kind") not in CLASS_KINDS:
            continue
        evidence = collect_role_evidence(class_id, types, draft)
        decision = decide_role(evidence)
        if decision.role != "none":
            draft.add_node(class_id, kind="system", role=decision.role, decided_by=decision.decided_by)
        # a class without a role still polls the event archetypes it holds (a game state, a subsystem)
        connect_event_edges(class_id, decision, draft)
        if decision.role == "undecided" and evidence.role_marker == "reactive" and evidence.table_anchored:
            draft.warn(f"marker value against the shape: {class_id} claims SystemRole(Reactive) on a table-anchored "
                       f"class — Reactive asks for the loop contract without a table anchor plus an event anchor or "
                       f"a held event archetype", "system/marker-value")
        elif decision.role == "undecided":
            draft.warn(f"marker needed: {class_id} is an Update-loop class holding an event archetype outside "
                       f"base(...) — its base does not decide per_frame or reactive; add [SystemRole]",
                       "system/marker-required")
        elif evidence.role_marker and decision.decided_by != "marker":
            draft.warn(f"redundant marker: {class_id} claims SystemRole, but its role {decision.role} is decided "
                       f"by {decision.decided_by}", "system/marker-forbidden")
    audit_cleanup(draft)


def audit_cleanup(draft):
    """One global cleanup system, running last in the tick, with no descendants — the whole of the cleanup law."""
    cleanups = sorted(i for i, n in draft.nodes.items() if n.get("role") == "cleanup")
    events_exist = any(n.get("kind") == "archetype" and n.get("main_tag") == EVENT_TAG for n in draft.nodes.values())
    if len(cleanups) > 1:
        draft.warn(f"cleanup: {len(cleanups)} cleanup systems [{', '.join(cleanups)}] — events are cleaned by ONE "
                   f"global system", "event/cleanup")
    elif not cleanups and events_exist:
        draft.warn("cleanup: events are raised but no system sweeps the event tag and deletes — ripe events leak",
                   "event/cleanup")
    for cleanup in cleanups:
        priority = draft.nodes[cleanup].get("priority")
        if priority != INT_MAX:
            draft.warn(f"cleanup: {cleanup} has Priority {priority} — the cleanup system runs last in the tick, at "
                       f"the largest possible integer", "event/cleanup")
        heirs = sorted({e.src for e in draft.edges if e.rel == "inherits" and e.dst == cleanup})
        if heirs:
            draft.warn(f"cleanup: {cleanup} has descendants [{', '.join(heirs)}] — the cleanup system has none, and "
                       f"no event gets a cleanup of its own", "event/cleanup")


def collect_role_evidence(class_id: str, types, draft) -> RoleEvidence:
    ancestors = types.ancestry.get(class_id, [])
    ancestor_names = {a.node for a in ancestors}
    node, tables = draft.nodes[class_id], draft.ecs_tables
    hosted = {e.dst for e in draft.edges if e.rel == "hosts"}
    marked = types.markers.get(class_id)
    return RoleEvidence(
        update_loop=bool(ancestor_names & UPDATE_LOOPS),
        sweeps_events=any(s["owner"] == class_id and EVENT_TAG in s["tags"] for s in tables["sets"])
                      and any(d["owner"] == class_id for d in tables["dispose_sites"]),
        anchored_on_event=node.get("base_anchor") == "event",
        table_anchored=node.get("base_anchor") == "table",
        holds_event_archetype=bool(node.get("held_events")),
        role_marker=marked.role if marked else None,
        pipeline_member=any(a.node == "IPrioritizedUniTaskSystem" and a.args == ("MapGenerationStep",)
                            for a in ancestors),
        turn_phase_member="TurnPhaseSubSystem" in ancestor_names,
        startup_step=any(a.node == "IUniTaskSystem" and not a.args for a in ancestors),
        family_member=any(a.node in hosted and draft.nodes.get(a.node, {}).get("abstract") for a in ancestors))


def decide_role(evidence: RoleEvidence) -> RoleDecision:
    """The first true branch wins — the order is the role decision of the cascade, word for word."""
    if evidence.update_loop and evidence.sweeps_events:
        return RoleDecision("cleanup", "lexical")
    if evidence.anchored_on_event:
        return RoleDecision("reactive", "base")
    if evidence.update_loop and evidence.holds_event_archetype:
        # the marker decides only when its value matches the shape of the class: Reactive asks for the loop contract
        # WITHOUT a table anchor, and MarkerShapeAnalyzer refuses the same combination in the Unity compilation
        if evidence.role_marker == "reactive" and evidence.table_anchored:
            return RoleDecision("undecided", "none")
        return RoleDecision(evidence.role_marker, "marker") if evidence.role_marker else RoleDecision("undecided", "none")
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


def connect_event_edges(class_id: str, decision: RoleDecision, draft):
    """A base-anchored event is what a reactive class reacts to; a held event archetype is reacted to only under the
    reactive marker, and polled otherwise."""
    node = draft.nodes[class_id]
    location = node.get("source_location", "")
    if decision.role == "reactive" and decision.decided_by == "base":
        for event in node.get("anchor_events", []):
            draft.add_edge(Edge(class_id, event, "reacts_to", "base(...)", location))
    held_rel = "reacts_to" if (decision.role, decision.decided_by) == ("reactive", "marker") else "polls"
    for event in node.get("held_events", []):
        draft.add_edge(Edge(class_id, event, held_rel, "held archetype", location))
