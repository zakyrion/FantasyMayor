using UnityEngine;
using Zenject;

public class HexesInstallers : MonoInstaller
{
    [SerializeField] private HexMonoFactory _hexMonoFactory;

    
    public override void InstallBindings()
    {
        Container.Bind<HexMonoFactory>().FromInstance(_hexMonoFactory);
        
        Container.BindInterfacesAndSelfTo<HexesAtom>().AsSingle();
    }
}
