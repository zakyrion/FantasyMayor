using System;

namespace Modules.Boot.Core
{
    [Flags]
    public enum AppState
    {
        Initialization = 1 << 0,
        ConfigLoading = 1 << 1,
        InstanceObjects = 1 << 2,
        MainMenu = 1 << 3,
        MapCreation = 1 << 4,
        MapLoading = 1 << 5,
        Gameplay = 1 << 6,
        GameOver = 1 << 7
    }
}
