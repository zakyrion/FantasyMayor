using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.Events
{
    /// <summary>
    ///     The mode someone asks the app to switch to; a request only — the current mode is known by the
    ///     machine alone.
    /// </summary>
    public struct AppStateRequestedEvent : IEventTag
    {
        public AppState Requested;
    }
}
