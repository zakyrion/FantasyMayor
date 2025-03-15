using UnityEngine;
using VContainer;

public class HexesInstallers : MonoBehaviour
{
    [SerializeField] private HexMonoFactory _hexMonoFactory;


    public void InstallBindings(IContainerBuilder builder)
    {
        builder.RegisterInstance(_hexMonoFactory);
        builder.Register<HexesSystem>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
    }
}