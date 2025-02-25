using Zenject;

public class PathfindingInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<PathfindingAtom>().AsSingle();
    }
}