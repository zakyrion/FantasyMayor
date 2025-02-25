using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "SignalsInstaller", menuName = "Installers/SignalsInstaller")]
public class SignalsInstaller : ScriptableObjectInstaller<SignalsInstaller>
{
    public override void InstallBindings()
    {
        InitGlobalServices();
        InitSignals();
    }

    private void InitGlobalServices()
    {
    }

    private void InitSignals()
    {
        SignalBusInstaller.Install(Container);
    }
}