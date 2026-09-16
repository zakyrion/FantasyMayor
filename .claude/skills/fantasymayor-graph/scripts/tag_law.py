"""The tag law — one main tag per archetype, unique to it (EventTag excepted), and label tags beside it that no
query filters by. Every deviation is named with its archetype or its place."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass

from ecs_facts import EVENT_FRAME, EVENT_TAG

LABEL_CAP = 4   # labels beside the main tag: the declaration takes at most five tag type arguments


@dataclass
class TagDeviation:
    kind: str
    archetype_or_owner: str
    tags: list | set
    source_location: str
    rule: str


def audit_tag_law(types, draft):
    def deviate(deviation: TagDeviation, message: str):
        draft.tag_audit.append({"kind": deviation.kind, "archetype_or_owner": deviation.archetype_or_owner,
                                "tags": sorted(deviation.tags), "source_location": deviation.source_location,
                                "rule": deviation.rule})
        draft.warn(f"tag law: {message} @ {deviation.source_location}", deviation.rule)

    def is_label(tag):
        return tag in types.markers and types.markers[tag].label is not None

    tags_of = defaultdict(set)
    for edge in draft.edges:
        if edge.rel == "has" and draft.nodes.get(edge.dst, {}).get("kind") == "tag":
            tags_of[edge.src].add(edge.dst)

    archetypes_of_main = defaultdict(list)
    for archetype_id, archetype in sorted(draft.nodes.items()):
        if archetype.get("kind") != "archetype":
            continue
        location = archetype.get("source_location", "")
        main_tags = sorted(t for t in tags_of[archetype_id] if not is_label(t))
        labels = sorted(t for t in tags_of[archetype_id] if is_label(t))
        written = [t for t in archetype.get("tag_order", []) if t]
        if len(labels) > LABEL_CAP:
            deviate(TagDeviation("label-count", archetype_id, labels, location, "tag/label-count"),
                    f"archetype {archetype_id} carries {len(labels)} label tags [{', '.join(labels)}] — beside the "
                    f"main tag stand from zero to {LABEL_CAP} labels")
        if not main_tags:
            deviate(TagDeviation("no-main-tag", archetype_id, tags_of[archetype_id], location, "tag/one-main-tag"),
                    f"archetype {archetype_id} has no main tag")
        elif len(main_tags) > 1:
            deviate(TagDeviation("several-main-tags", archetype_id, main_tags, location, "tag/one-main-tag"),
                    f"archetype {archetype_id} carries several main tags [{', '.join(main_tags)}]")
        else:
            archetypes_of_main[main_tags[0]].append(archetype_id)
            if written and written[0] != main_tags[0]:   # exactly one main tag, and it is written first
                deviate(TagDeviation("main-tag-not-first", archetype_id, main_tags, location, "tag/one-main-tag"),
                        f"archetype {archetype_id} writes label tag {written[0]} before its main tag {main_tags[0]}")
        if EVENT_TAG in tags_of[archetype_id] and EVENT_FRAME not in archetype.get("components", []):
            deviate(TagDeviation("event-tag-outside-event", archetype_id, [EVENT_TAG], location,
                                 "event/no-tag-on-persistent-row"),
                    f"archetype {archetype_id} carries EventTag without EventFrameComponent")

    for tag, archetypes in sorted(archetypes_of_main.items()):
        if tag != EVENT_TAG and len(archetypes) > 1:
            deviate(TagDeviation("shared-main-tag", ", ".join(archetypes), [tag],
                                 draft.nodes[tag].get("source_location", ""), "tag/main-tag-unique"),
                    f"main tag {tag} is shared by [{', '.join(archetypes)}]")

    # the other half of the event-tag law: every event carries the event tag as its main tag
    with_event_tag = {c for n in draft.nodes.values() if n.get("kind") == "archetype" and n.get("main_tag") == EVENT_TAG
                      for c in n.get("components", [])}
    for event_id, event in sorted(draft.nodes.items()):
        if event.get("kind") == "event" and event.get("declared") and event_id not in with_event_tag:
            deviate(TagDeviation("event-without-event-tag", event_id, [EVENT_TAG],
                                 event.get("source_location", ""), "tag/event-tag"),
                    f"event {event_id} is carried by no archetype whose main tag is EventTag")

    tables = draft.ecs_tables
    for query in (s for s in tables["sets"] if EVENT_TAG not in s["tags"]):   # an event sweep filters by EventTag
        labels = [t for t in query["tags"] if is_label(t)]
        if labels:
            deviate(TagDeviation("label-in-filter", query["owner"], labels, query["source_location"],
                                 "tag/label-not-a-filter"),
                    f"{query['owner']} filters by label tag(s) [{', '.join(labels)}] in {query['via']}")
        if len(query["tags"]) != 1:
            deviate(TagDeviation("filter-tag-count", query["owner"], query["tags"], query["source_location"],
                                 "table/filter"),
                    f"{query['owner']} filters across archetypes with {len(query['tags'])} tags in {query['via']}")
    for write in tables["late_writes"]:
        deviate(TagDeviation("runtime-tag-write", write["owner"], [write["component"]], write["source_location"],
                             "tag/added-by-archetype-only"),
                f"{write['owner']} adds tag {write['component']} during the entity's life")
    for add in tables["tags_add_sites"]:
        deviate(TagDeviation("tags-add", add["owner"], add["tags"], add["source_location"],
                             "tag/composed-by-declaration"),
                f"{add['owner']} composes tags through Tags.Add — not read")
