using DefaultEcs;
using JetBrains.Annotations;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // ДІЇ populator (dormant scaffold): the district-action model is a later slice, so there is nothing to
    // reconcile yet. Exists so the section family stays uniform and the seam is wired; Populate is a no-op.
    [UsedImplicitly]
    public sealed class DistrictBuildActionsUISubSystem : DistrictBuildUISubSystem
    {
        private const int ExecutionPriority = 400;

        public override int Priority => ExecutionPriority;

        public DistrictBuildActionsUISubSystem(World world) : base(world)
        {
        }

        public override void Populate(GameObject root)
        {
        }
    }
}
