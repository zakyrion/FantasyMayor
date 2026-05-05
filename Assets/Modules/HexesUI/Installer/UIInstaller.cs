using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.HexesUI.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.HexesUI.Installer
{
    /// <summary>Registers UI services and the <see cref="FirstUIStep" /> boot systems.</summary>
    public class UIInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ShowHexesUISystem>(Lifetime.Scoped)
                .As<ShowHexesUISystem, IUniTaskSystem<FirstUIStep>>();
        }
    }
}
