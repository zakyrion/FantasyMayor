using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.MainCanvas.Core;
using Presentation.UI.MainHud.Components;

namespace Presentation.UI.MainHud.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 800). Instantiates the shared Main UI prefab (<c>UI/MainUI</c>,
    ///     which carries every window's view) under the main canvas, then hands that GameObject to each
    ///     registered <see cref="MainHudSpawnSubSystem" /> in priority order so it can resolve its view and
    ///     publish its component. Owns the single addressable handle for the whole Main UI.
    /// </summary>
    [UsedImplicitly]
    internal sealed class MainHudSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const string MainUIPath = "MainHUD";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly IReadOnlyList<MainHudSpawnSubSystem> _subSystems;
        private readonly World _world;

        public int Priority => SystemPriorities.WorldInit.MainHudSpawn;

        public MainHudSpawnSystem(World world,
            IAddressable addressable,
            IMainCanvasProvider canvasProvider,
            IReadOnlyList<MainHudSpawnSubSystem> subSystems)
        {
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (_world.Has<MainHudComponent>())
                return;

            var canvas = _canvasProvider.RootGO;

            if (cancellationToken.IsCancellationRequested)
                return;

            var result = await _addressable.LoadAndInstanceAsync(MainUIPath, cancellationToken, canvas.transform);

            if (cancellationToken.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            if (result.Status != Status.Success || !result.Box.Exist)
                throw new InvalidOperationException(
                    $"MainHudSpawnSystem: failed to load Main UI by address '{MainUIPath}'.");

            _world.Set(new MainHudComponent
            {
                RootBox = result.Box
            });

            // Subsystems do not instantiate — each pulls its view off the shared Main UI instance.
            foreach (var subSystem in _subSystems)
            {
                if (!subSystem.IsEnabled)
                    continue;

                subSystem.Prepare(result.Box.Value);
            }
        }

        public void Dispose()
        {
            if (_world != null && _world.Has<MainHudComponent>())
            {
                _world.Get<MainHudComponent>().RootBox.Dispose();
                _world.Remove<MainHudComponent>();
            }
        }
    }
}
