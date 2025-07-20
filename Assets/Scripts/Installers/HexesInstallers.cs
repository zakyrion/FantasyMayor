using Modules.Hexes;
using Modules.Hexes.Creators;
using Modules.Hexes.Systems;
using Modules.Hexes.ViewManagers;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class HexesInstallers : MonoInstaller
    {
        [SerializeField]
        private GameObject _hexesRoot;

        public override void InstallBindings()
        {
            Container.BindInstance(_hexesRoot).WithId(Constants.HEXES_ROOT_ID).AsCached();

            Container.BindInterfacesTo<HexesSystem>().AsCached().NonLazy();
            Container.BindInterfacesTo<HexesGenerateProcessor>().AsCached().NonLazy();
            Container.BindInterfacesTo<HexesViewManager>().AsCached().NonLazy();
            Container.BindInterfacesTo<HexesCreator>().AsCached();
        }
    }
}
