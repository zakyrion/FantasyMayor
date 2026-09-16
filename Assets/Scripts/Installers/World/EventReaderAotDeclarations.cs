using System;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Events;
using Domains.Map.Generation.Components;
using EcsExtensions;
using Flows.DistrictBuild.Events;
using Modules.Boot.Implementation.Events;
using Modules.Turn.Events;
using Presentation.HexIcons.Events;
using Presentation.HexResources.Events;
using Presentation.Terrain.Events;
using Presentation.UI.MainHud.ContextTabs.Events;
using UnityEngine.Scripting;

namespace Installers.World
{
    /// <summary>
    ///     Statically mentions the closed <see cref="EventReader{TEvent}" /> of every event type so IL2CPP
    ///     generates its code — the open generic registration in <see cref="WorldInstaller" /> stays the only
    ///     source readers are actually built from in the running game. Add the new type here too whenever a
    ///     new event type is declared (rule event/aot-reader). Never called: the throw at the end keeps the
    ///     compiler from discarding this method as dead code before the closed types are mentioned.
    /// </summary>
    [Preserve]
    internal static class EventReaderAotDeclarations
    {
        [Preserve]
        private static void DeclareClosedReaders(EntityStorages storages)
        {
            var selectedHexChanges = new EventReader<SelectedHexChangedEvent>(storages);
            selectedHexChanges.TryRead(out _);
            selectedHexChanges.DrainBatch();

            var hexIconsVisibilityChanges = new EventReader<HexIconsVisibilityChangedEvent>(storages);
            hexIconsVisibilityChanges.TryRead(out _);
            hexIconsVisibilityChanges.DrainBatch();

            var forestHexAppearances = new EventReader<ForestHexAppearedEvent>(storages);
            forestHexAppearances.TryRead(out _);
            forestHexAppearances.DrainBatch();

            var forestHexRemovals = new EventReader<ForestHexRemovedEvent>(storages);
            forestHexRemovals.TryRead(out _);
            forestHexRemovals.DrainBatch();

            var contextTabChanges = new EventReader<ContextTabChangedEvent>(storages);
            contextTabChanges.TryRead(out _);
            contextTabChanges.DrainBatch();

            var turnCompletions = new EventReader<TurnCompletedEvent>(storages);
            turnCompletions.TryRead(out _);
            turnCompletions.DrainBatch();

            var nextTurnRequests = new EventReader<NextTurnEvent>(storages);
            nextTurnRequests.TryRead(out _);
            nextTurnRequests.DrainBatch();

            var overlayRequests = new EventReader<DistrictBuildUIRequestedEvent>(storages);
            overlayRequests.TryRead(out _);
            overlayRequests.DrainBatch();

            var buildConfirmations = new EventReader<DistrictBuildConfirmedEvent>(storages);
            buildConfirmations.TryRead(out _);
            buildConfirmations.DrainBatch();

            var buildCancellations = new EventReader<BuildDistrictCancelEvent>(storages);
            buildCancellations.TryRead(out _);
            buildCancellations.DrainBatch();

            var buildCompletions = new EventReader<BuildDistrictCompleteEvent>(storages);
            buildCompletions.TryRead(out _);
            buildCompletions.DrainBatch();

            var districtTableChanges = new EventReader<DistrictTableChangedEvent>(storages);
            districtTableChanges.TryRead(out _);
            districtTableChanges.DrainBatch();

            var generateRequests = new EventReader<TerrainGenerationGenerateEventComponent>(storages);
            generateRequests.TryRead(out _);
            generateRequests.DrainBatch();

            var appStateRequests = new EventReader<AppStateRequestedEvent>(storages);
            appStateRequests.TryRead(out _);
            appStateRequests.DrainBatch();

            throw new InvalidOperationException("AOT declaration only — never called");
        }
    }
}
