using Modules.Addressable.Core;
using VContainer;
using VContainer.Unity;

namespace Installers.Addressable
{
    public class AddressableInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IAddressable, Modules.Addressable.Implementation.Addressable>(Lifetime.Scoped);
        }
    }
}
