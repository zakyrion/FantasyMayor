using System;
using System.Collections.Generic;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.Turn.Installer
{
    public sealed class TurnInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // No turn phases exist yet — inject an explicit empty list so the processor resolves cleanly.
            // When the first PhaseNSubSystem lands, remove this line and register each phase
            // .As<PhaseN, TurnPhaseSubSystem>(), as HexResourcesViewInstaller does for view subsystems.
            builder.RegisterInstance<IReadOnlyList<TurnPhaseSubSystem>>(Array.Empty<TurnPhaseSubSystem>());

            builder.Register<TurnProcessorSystem>(Lifetime.Singleton)
                .As<TurnProcessorSystem>();
        }
    }
}
