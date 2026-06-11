namespace Modules.HexResourcesView.Events
{
    /// <summary>
    ///     One-frame generic pulse: forest resources changed (something may have appeared). Carries no
    ///     payload — <see cref="Systems.ForestSpawnSystem" /> reconciles the whole forest state against the
    ///     view state, so the signal only needs to *exist*. Paired with EventTag; disposed each tick by
    ///     EventCleanupSystem. No emitter wires it yet — runtime planting is future gameplay.
    /// </summary>
    public struct ForestHexAppearedEvent
    {
    }
}
