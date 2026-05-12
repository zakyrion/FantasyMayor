using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Modules.Boot.Core;
using UnityEngine;
using VContainer;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Entry point MonoBehaviour. Runs boot phases sequentially on startup,
    ///     then drives the per-frame <see cref="IUpdatedSystem" /> loop each Unity Update tick.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        private IReadOnlyList<IUniTaskSystem<ConfigLoadStep>> _configLoadSystems;
        private IReadOnlyList<IUniTaskSystem<FirstUIStep>> _firstUISystems;
        private bool _isBootComplete;
        private IReadOnlyList<IUpdatedSystem> _updatedSystems;

        private async UniTask Start()
        {
            Debug.Log($"Starting {GetType().Name}");

            Application.targetFrameRate = 60;

            var loadConfigs = new UniTaskSequentialSystem<ConfigLoadStep>(_configLoadSystems);
            await loadConfigs.Update(new ConfigLoadStep(), CancellationToken.None);

            var firstUI = new UniTaskSequentialSystem<FirstUIStep>(_firstUISystems);
            await firstUI.Update(new FirstUIStep(), CancellationToken.None);

            _isBootComplete = true;
        }

        private void Update()
        {
            if (!_isBootComplete)
                return;

            var state = new GameState(Time.deltaTime);

            foreach (var updatedSystem in _updatedSystems)
                updatedSystem.Update(state);
        }

        private void OnDestroy()
        {
            if (_updatedSystems == null)
                return;

            foreach (var updatedSystem in _updatedSystems)
                updatedSystem.Dispose();
        }

        /// <summary>Receives all systems bound to each boot phase and the runtime update loop via VContainer.</summary>
        /// <param name="configLoadSystems">Systems for the config-load boot phase.</param>
        /// <param name="firstUISystems">Systems for the first-UI boot phase.</param>
        /// <param name="updatedSystems">Per-frame systems, sorted ascending by <see cref="IUpdatedSystem.Priority" />.</param>
        [Inject]
        public void Construct(
            World world,
            IReadOnlyList<IUniTaskSystem<ConfigLoadStep>> configLoadSystems,
            IReadOnlyList<IUniTaskSystem<FirstUIStep>> firstUISystems,
            IReadOnlyList<IUpdatedSystem> updatedSystems)
        {
            _configLoadSystems = configLoadSystems;
            _firstUISystems = firstUISystems;
            _updatedSystems = updatedSystems
                .OrderBy(s => s.Priority)
                .ToArray();
        }
    }
}
