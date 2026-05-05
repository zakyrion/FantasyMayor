using Modules.MainCanvas.Core;
using Modules.MainCanvas.Implementation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Modules.MainCanvas.Installer
{
    public class MainCanvasInstaller : LifetimeScope
    {
        [SerializeField]
        private GameObject _uiRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IMainCanvasProvider, MainCanvasProvider>(Lifetime.Scoped).WithParameter(_uiRoot);
        }
    }
}
