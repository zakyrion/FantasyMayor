using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.MainCanvas.Core;
using UnityEngine;

namespace Modules.MainUI.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 800). Instantiates the shared Main UI prefab (<c>UI/MainUI</c>,
    ///     which carries every window's view) under the main canvas, then hands that GameObject to each
    ///     registered <see cref="MainUISpawnSubSystem" /> in priority order so it can resolve its view and
    ///     publish its component. Owns the single addressable handle for the whole Main UI.
    /// </summary>
    [UsedImplicitly]
    internal sealed class MainUISpawnSystem : IPrioritizedUniTaskSystem<TerrainGenerationStep>
    {
        private const int ExecutionPriority = 800;
        private const string MainUIPath = "UI/MainUI";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly IReadOnlyList<MainUISpawnSubSystem> _subSystems;

        private Box<GameObject> _rootBox;
        private bool _isDisposed;
        private bool _isLoaded;

        public int Priority => ExecutionPriority;

        public MainUISpawnSystem(
            IAddressable addressable,
            IMainCanvasProvider canvasProvider,
            IReadOnlyList<MainUISpawnSubSystem> subSystems)
        {
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
            _rootBox = Box<GameObject>.Empty();
        }

        public async UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (_isLoaded)
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
                    $"MainUISpawnSystem: failed to load Main UI by address '{MainUIPath}'.");

            _rootBox = result.Box;
            var mainUi = _rootBox.Value;

            // Subsystems do not instantiate — each pulls its view off the shared Main UI instance.
            foreach (var subSystem in _subSystems)
            {
                if (!subSystem.IsEnabled)
                    continue;

                subSystem.Prepare(mainUi);
            }

            _isLoaded = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_rootBox.Exist)
                _rootBox.Dispose();
        }
    }
}
