using System;
using DefaultEcs;
using Domains.Actors.Data;

namespace Domains.Actions.ResourceSpend.Systems
{
    // Base of the per-owner resource-spend family (PATTERN_ORCHESTRATOR_SUBSYSTEM, routing variant). A plain
    // IDisposable, NOT an ECS system: it may hold owner-keyed query caches. Each concrete owns exactly one owner
    // type's resource stacks; the ResourceSpender orchestrator routes a spend to the matching owner. The actual
    // afford/deduct arithmetic lives in Economy's ResourceLedger — this layer only resolves whose stacks to hand it.
    public abstract class ResourceSpendSubSystem : IDisposable
    {
        protected readonly World World;

        protected ResourceSpendSubSystem(World world) => World = world;

        // Which owner this subsystem spends resources from.
        public abstract ActorType Owner { get; }

        // The mayor is the game's sole Action-Point pool; only that subsystem overrides these two.
        public virtual bool HandlesActionPoints => false;

        public virtual bool TryGetActionPointStacks(out ReadOnlySpan<Entity> stacks) =>
            throw new InvalidOperationException($"{GetType().Name} owns no Action-Point pool.");

        // Resolve this owner's resource stacks (one entity per held ResourceType). false ⇒ owner has no stacks.
        public abstract bool TryGetStacks(int ownerId, out ReadOnlySpan<Entity> stacks);

        public virtual void Dispose() { }
    }
}
