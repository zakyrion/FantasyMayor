"""The tag law — one main tag per archetype, unique to it (EventTag excepted), and label tags beside it that no
query filters by. Every deviation is named with its archetype or its place."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass

from ecs_facts import EVENT_FRAME, EVENT_TAG


@dataclass
class TagDeviation:
    kind: str
    archetype_or_owner: str
    tags: list | set
    source_location: str


def audit_tag_law(types, draft):
    def deviate(deviation: TagDeviation, message: str):
        draft.tag_audit.append({"kind": deviation.kind, "archetype_or_owner": deviation.archetype_or_owner,
                                "tags": sorted(deviation.tags), "source_location": deviation.source_location})
        draft.warn(f"tag law: {message} @ {deviation.source_location}")

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
        if not main_tags:
            deviate(TagDeviation("no-main-tag", archetype_id, tags_of[archetype_id], location),
                    f"archetype {archetype_id} has no main tag")
        elif len(main_tags) > 1:
            deviate(TagDeviation("several-main-tags", archetype_id, main_tags, location),
                    f"archetype {archetype_id} carries several main tags [{', '.join(main_tags)}]")
        else:
            archetypes_of_main[main_tags[0]].append(archetype_id)
        if EVENT_TAG in tags_of[archetype_id] and EVENT_FRAME not in archetype.get("components", []):
            deviate(TagDeviation("event-tag-outside-event", archetype_id, [EVENT_TAG], location),
                    f"archetype {archetype_id} carries EventTag without EventFrameComponent")

    for tag, archetypes in sorted(archetypes_of_main.items()):
        if tag != EVENT_TAG and len(archetypes) > 1:
            deviate(TagDeviation("shared-main-tag", ", ".join(archetypes), [tag],
                                 draft.nodes[tag].get("source_location", "")),
                    f"main tag {tag} is shared by [{', '.join(archetypes)}]")

    tables = draft.ecs_tables
    for query in (s for s in tables["sets"] if EVENT_TAG not in s["tags"]):   # an event sweep filters by EventTag
        labels = [t for t in query["tags"] if is_label(t)]
        if labels:
            deviate(TagDeviation("label-in-filter", query["owner"], labels, query["source_location"]),
                    f"{query['owner']} filters by label tag(s) [{', '.join(labels)}] in {query['via']}")
        if len(query["tags"]) != 1:
            deviate(TagDeviation("filter-tag-count", query["owner"], query["tags"], query["source_location"]),
                    f"{query['owner']} filters across archetypes with {len(query['tags'])} tags in {query['via']}")
    for write in tables["late_writes"]:
        deviate(TagDeviation("runtime-tag-write", write["owner"], [write["component"]], write["source_location"]),
                f"{write['owner']} adds tag {write['component']} during the entity's life")
    for add in tables["tags_add_sites"]:
        deviate(TagDeviation("tags-add", add["owner"], add["tags"], add["source_location"]),
                f"{add['owner']} composes tags through Tags.Add — not read")
