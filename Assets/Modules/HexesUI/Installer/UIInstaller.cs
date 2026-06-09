using Modules.HexesUI.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.HexesUI.Installer
{
    /// <summary>Registers the hex generator UI system. It is driven by the MainMenu game state, not a boot phase.</summary>
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ShowHexesUISystem>(Lifetime.Scoped)
                .As<ShowHexesUISystem>();
        }
    }
}
