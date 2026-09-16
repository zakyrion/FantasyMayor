using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     One game state's first-order systems, split by kind and ordered for its three runs: the awaited entry
    ///     (<see cref="RunEntryAsync" />), the per-frame tick (<see cref="Tick" />) and the per-late-frame tick
    ///     (<see cref="LateTick" />). Built once by <see cref="Filter" /> from every system whose
    ///     <see cref="IAppStateSystem.AppState" /> flags contain the state's mode.
    /// </summary>
    public sealed class AppStateSystems
    {
        public IReadOnlyList<IUpdatedSystem> UpdatedSystems { get; }
        public IReadOnlyList<ILateUpdatedSystem> LateUpdatedSystems { get; }
        public IReadOnlyList<IUniTaskSystem> EntrySteps { get; }
        public IReadOnlyList<IPipelineStageSystem> PipelineStages { get; }

        private AppStateSystems(
            IReadOnlyList<IUpdatedSystem> updatedSystems,
            IReadOnlyList<ILateUpdatedSystem> lateUpdatedSystems,
            IReadOnlyList<IUniTaskSystem> entrySteps,
            IReadOnlyList<IPipelineStageSystem> pipelineStages)
        {
            UpdatedSystems = updatedSystems;
            LateUpdatedSystems = lateUpdatedSystems;
            EntrySteps = entrySteps;
            PipelineStages = pipelineStages;
        }

        /// <summary>
        ///     Keeps the systems of <paramref name="allSystems" /> whose flags contain <paramref name="mode" /> and
        ///     files each into the list of its most specific kind: a pipeline stage into <see cref="PipelineStages" />,
        ///     any other one-shot system into <see cref="EntrySteps" />; independently, an updated system into
        ///     <see cref="UpdatedSystems" /> and a late-updated system into <see cref="LateUpdatedSystems" /> — so a
        ///     system of both kinds lands in both.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     A kept system implements none of the four kinds — the registration rule does not catch this case
        ///     (s1 contra :c-4 residual).
        /// </exception>
        public static AppStateSystems Filter(AppState mode, IReadOnlyList<IAppStateSystem> allSystems)
        {
            var updatedSystems = new List<IUpdatedSystem>();
            var lateUpdatedSystems = new List<ILateUpdatedSystem>();
            var entrySteps = new List<IUniTaskSystem>();
            var pipelineStages = new List<IPipelineStageSystem>();

            foreach (var system in allSystems)
            {
                if ((system.AppState & mode) == 0)
                    continue;

                var filedIntoAKind = false;

                if (system is IPipelineStageSystem pipelineStage)
                {
                    pipelineStages.Add(pipelineStage);
                    filedIntoAKind = true;
                }
                else if (system is IUniTaskSystem entryStep)
                {
                    entrySteps.Add(entryStep);
                    filedIntoAKind = true;
                }

                if (system is IUpdatedSystem updatedSystem)
                {
                    updatedSystems.Add(updatedSystem);
                    filedIntoAKind = true;
                }

                if (system is ILateUpdatedSystem lateUpdatedSystem)
                {
                    lateUpdatedSystems.Add(lateUpdatedSystem);
                    filedIntoAKind = true;
                }

                if (!filedIntoAKind)
                    throw new InvalidOperationException(
                        $"{system.GetType().Name} is flagged for {mode} but implements none of the kinds {mode} runs.");
            }

            return new AppStateSystems(
                updatedSystems.OrderBy(system => system.Priority).ToArray(),
                lateUpdatedSystems.OrderBy(system => system.Priority).ToArray(),
                entrySteps.ToArray(),
                pipelineStages.OrderBy(stage => stage.Priority).ToArray());
        }

        /// <summary>Awaits every entry step in registration order, then every pipeline stage in priority order.</summary>
        /// <exception cref="OperationCanceledException">The entry was cancelled.</exception>
        public async UniTask RunEntryAsync(CancellationToken cancellationToken)
        {
            foreach (var entryStep in EntrySteps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await entryStep.Execute(cancellationToken);
            }

            foreach (var pipelineStage in PipelineStages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await pipelineStage.Execute(cancellationToken);
            }
        }

        /// <summary>Ticks every kept updated system in priority order.</summary>
        public void Tick(GameState state)
        {
            for (var i = 0; i < UpdatedSystems.Count; i++)
                UpdatedSystems[i].Update(state);
        }

        /// <summary>Ticks every kept late-updated system in priority order.</summary>
        public void LateTick(GameState state)
        {
            for (var i = 0; i < LateUpdatedSystems.Count; i++)
                LateUpdatedSystems[i].Update(state);
        }
    }
}
