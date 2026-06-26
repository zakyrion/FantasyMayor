using VContainer;
using VContainer.Unity;

namespace Domains.Economy.Installer
{
    // Economy ships no systems yet: Resource is data types + the generic ResourceLoadoutSpawner mechanism,
    // and District is a data-only scaffold. District build/operate systems will register here later.
    public sealed class EconomyInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
        }
    }
}
