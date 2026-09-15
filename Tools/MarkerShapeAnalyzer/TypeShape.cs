using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The actual shape of a marked type — what the marker claims is measured against.
    internal sealed class TypeShape
    {
        public readonly bool LoopContract;
        public readonly bool EventAnchored;
        public readonly bool TableAnchored;
        public readonly ImmutableArray<IMethodSymbol> HeldEventArchetypes;
        public readonly ImmutableArray<IEventSymbol> SubscribedEvents;
        public readonly bool TagStruct;

        public TypeShape(bool loopContract, bool eventAnchored, bool tableAnchored,
            ImmutableArray<IMethodSymbol> heldEventArchetypes, ImmutableArray<IEventSymbol> subscribedEvents, bool tagStruct)
        {
            LoopContract = loopContract;
            EventAnchored = eventAnchored;
            TableAnchored = tableAnchored;
            HeldEventArchetypes = heldEventArchetypes;
            SubscribedEvents = subscribedEvents;
            TagStruct = tagStruct;
        }
    }
}
