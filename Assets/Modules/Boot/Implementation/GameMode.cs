namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     High-level game states driven by <see cref="GameModeMachine" />. Only one is active at a time;
    ///     each owns the set of systems that run while it is active (wired manually in Boot).
    /// </summary>
    public enum GameMode
    {
        MainMenu,
        MapCreation,
        MapLoading,
        Gameplay
    }
}
