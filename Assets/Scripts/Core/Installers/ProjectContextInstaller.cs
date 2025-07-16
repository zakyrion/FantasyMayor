using Core.DataLayer;
using Core.EventDataBus;
using JetBrains.Annotations;
using Modules.Addressable;
using Zenject;

namespace Core.Installers
{
    [UsedImplicitly]
    public class ProjectContextInstaller : MonoInstaller<ProjectContextInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<Addressable>().AsSingle().NonLazy();
            Container.Bind(typeof(IBus<>)).To(typeof(Bus<>)).AsCached();
            Container.Bind(typeof(IDataContainer<>)).To(typeof(DataContainer<>)).AsCached();
        }
    }
}
