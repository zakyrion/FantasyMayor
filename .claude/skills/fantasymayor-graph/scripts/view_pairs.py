"""View ↔ subscriber pairs: a class marked [ViewSubscriber(typeof(V))] is joined to V by its own C# subscription to an
event V (or a base of V) declares; a subscription to a view event without the marker is named, never guessed."""
from __future__ import annotations

from graph_draft import Edge

UNMARKED_SUBSCRIPTION_WARNING = "subscription to a view event without a marker:"


def pair_view_subscribers(sources, types, draft):
    declared_events = {}
    for declaration in sources.declarations:
        declared_events.setdefault(declaration["id"], set()).update(declaration["events"])
    view_events = {}   # event name -> ids of the views that declare it, themselves or through a base
    for view_id in (i for i, n in draft.nodes.items() if n.get("kind") == "view"):
        lineage = [view_id, *(a.node for a in types.ancestry.get(view_id, []))]
        for event in set().union(*(declared_events.get(i, set()) for i in lineage)):
            view_events.setdefault(event, set()).add(view_id)

    paired = set()
    for subscriber, marked in types.markers.items():
        for view in marked.views:
            subscriptions = [(i, s) for i, s in enumerate(sources.subscription_sites)
                             if s["owner"] == subscriber and view in view_events.get(s["event"], ())]
            for index, subscription in subscriptions:
                draft.add_edge(Edge(subscriber, view, "subscribes", f"{subscription['event']} +=",
                                    subscription["source_location"]))
                paired.add(index)
            if not subscriptions:
                draft.warn(f"view marker without a subscription: {subscriber} claims ViewSubscriber({view})")

    for index, subscription in enumerate(sources.subscription_sites):
        views = view_events.get(subscription["event"], set())
        if index in paired or not views or not subscription["owner"]:
            continue
        if len(views) == 1:
            draft.warn(f"{UNMARKED_SUBSCRIPTION_WARNING} {subscription['owner']} "
                       f"{subscription['receiver']}.{subscription['event']} += ({next(iter(views))}) "
                       f"@ {subscription['source_location']}")
        else:
            draft.warn(f"AMBIGUOUS view event {subscription['event']} [{', '.join(sorted(views))}] — "
                       f"{subscription['owner']} @ {subscription['source_location']}")
