using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The actual shape of a type — what a marker on it is measured against, and what decides whether it needs one.
    internal sealed class TypeShape
    {
        public readonly bool LoopContract;
        public readonly bool HoldsEventReader;
        public readonly ImmutableArray<IEventSymbol> SubscribedEvents;
        public readonly bool TagStruct;

        public TypeShape(bool loopContract, bool holdsEventReader, ImmutableArray<IEventSymbol> subscribedEvents,
            bool tagStruct)
        {
            LoopContract = loopContract;
            HoldsEventReader = holdsEventReader;
            SubscribedEvents = subscribedEvents;
            TagStruct = tagStruct;
        }
    }
}
