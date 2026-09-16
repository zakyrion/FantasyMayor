using Modules.Boot.Core;
using VContainer;

namespace EcsExtensions
{
    /// <summary>
    ///     The one registration rule of every first-order system: it is exposed as <see cref="IAppStateSystem" />
    ///     and handed the game states it belongs to. A system that never registers through this helper can never
    ///     reach a state — the constraint on <typeparamref name="TSystem" /> makes a system without an
    ///     <see cref="IAppStateSystem.AppState" /> a compile error, and the helper makes a registration that skips
    ///     <see cref="IAppStateSystem" /> impossible.
    /// </summary>
    public static class AppStateSystemRegistration
    {
        /// <summary>Registers a first-order system so it can reach the game states named by <paramref name="appState" />.</summary>
        /// <param name="builder">The container builder the registration line belongs to.</param>
        /// <param name="lifetime">The system's VContainer lifetime.</param>
        /// <param name="appState">The game states this system belongs to.</param>
        /// <returns>The registration builder, so a config loader can chain a further <c>WithParameter</c>.</returns>
        public static RegistrationBuilder RegisterAppStateSystem<TSystem>(this IContainerBuilder builder, Lifetime lifetime, AppState appState)
            where TSystem : IAppStateSystem
            => builder.Register<TSystem>(lifetime).As<IAppStateSystem>().WithParameter(appState);
    }
}
