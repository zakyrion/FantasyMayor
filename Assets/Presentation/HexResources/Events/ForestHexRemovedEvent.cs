using Friflo.Engine.ECS;
namespace Presentation.HexResources.Events
{
    /// <summary>
    ///     One-frame generic pulse: forest resources changed (something may have been removed). Carries no
    ///     payload — <see cref="Systems.ForestDespawnSystem" /> reconciles forest views against the current
    ///     forest-resource state, so the signal only needs to *exist*. Paired with EventTag; disposed each
    ///     tick by EventCleanupSystem. No emitter wires it yet — runtime chopping is future gameplay.
    /// </summary>
    public struct ForestHexRemovedEvent : IComponent
    {
    }
}
