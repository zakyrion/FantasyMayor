using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;

namespace Presentation.UI.DistrictBuild.Systems
{
    // ДІЇ populator (dormant scaffold): the district-action model is a later slice, so there is nothing to
    // reconcile yet. Exists so the section family stays uniform and the seam is wired; Update is a no-op.
    [UsedImplicitly]
    public sealed class DistrictBuildActionsUISubSystem : DistrictBuildUISubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.Actions;

        public DistrictBuildActionsUISubSystem(EntityStorages storages) : base(storages.World)
        {
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
