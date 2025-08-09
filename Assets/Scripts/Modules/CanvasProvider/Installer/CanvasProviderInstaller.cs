using Modules.CanvasProvider.Models;
using UnityEngine;
using Zenject;

namespace Modules.CanvasProvider.Installer
{
    public class CanvasProviderInstaller : MonoInstaller
    {
        [SerializeField]
        private GameObject _uiRoot;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<CanvasProviderModel>().AsCached().WithArguments(_uiRoot).NonLazy();
        }
    }
}
