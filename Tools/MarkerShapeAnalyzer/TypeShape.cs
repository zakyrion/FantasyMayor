using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The actual shape of a type — what a marker on it is measured against, and what decides whether it needs one.
    internal sealed class TypeShape
    {
        public readonly bool LoopContract;
        public readonly bool EventAnchored;
        public readonly bool TableAnchored;
        public readonly ImmutableArray<IMethodSymbol> HeldEventArchetypes;
        public readonly ImmutableArray<IEventSymbol> SubscribedEvents;
        public readonly bool TagStruct;
        public readonly bool Cleanup;

        public TypeShape(bool loopContract, bool eventAnchored, bool tableAnchored,
            ImmutableArray<IMethodSymbol> heldEventArchetypes, ImmutableArray<IEventSymbol> subscribedEvents,
            bool tagStruct, bool cleanup)
        {
            LoopContract = loopContract;
            EventAnchored = eventAnchored;
            TableAnchored = tableAnchored;
            HeldEventArchetypes = heldEventArchetypes;
            SubscribedEvents = subscribedEvents;
            TagStruct = tagStruct;
            Cleanup = cleanup;
        }
    }
}
