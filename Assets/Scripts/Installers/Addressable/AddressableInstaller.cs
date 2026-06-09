using Modules.Addressable.Core;
using VContainer;
using VContainer.Unity;

namespace Installers.Addressable
{
    public sealed class AddressableInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAddressable, Modules.Addressable.Implementation.Addressable>(Lifetime.Scoped);
        }
    }
}
