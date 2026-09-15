"""The role of every non-abstract class: from its base where the base decides, from its [SystemRole] marker where it
does not — and the event edges that follow from the role: reacts_to or polls."""
from __future__ import annotations

from dataclasses import dataclass

from ecs_facts import EVENT_TAG
from graph_draft import Edge

CLASS_KINDS = {"other", "installer", "config", "view"}
UPDATE_LOOPS = {"IUpdatedSystem", "ILateUpdatedSystem"}


@dataclass
class RoleEvidence:
    update_loop: bool
    sweeps_events: bool
    anchored_on_event: bool
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
        if decision.role == "undecided":
            draft.warn(f"marker needed: {class_id} is an Update-loop class holding an event archetype outside "
                       f"base(...) — its base does not decide per_frame or reactive; add [SystemRole]")
        elif evidence.role_marker and decision.decided_by != "marker":
            draft.warn(f"redundant marker: {class_id} claims SystemRole, but its role {decision.role} is decided "
                       f"by {decision.decided_by}")


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
