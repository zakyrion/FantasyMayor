using Core.DataLayer;
using Core.EventDataBus;
using JetBrains.Annotations;
using Zenject;

namespace Core.Installers
{
    [UsedImplicitly]
    public class ProjectContextInstaller : MonoInstaller<ProjectContextInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IBus<>)).To(typeof(Bus<>)).AsCached();
            Container.Bind(typeof(IDataContainer<>)).To(typeof(DataContainer<>)).AsCached();
        }
    }
}
