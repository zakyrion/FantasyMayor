using Friflo.Engine.ECS;
using JetBrains.Annotations;
using UnityEngine;
using EcsExtensions;

namespace Presentation.UI.DistrictBuild.Systems
{
    // ДІЇ populator (dormant scaffold): the district-action model is a later slice, so there is nothing to
    // reconcile yet. Exists so the section family stays uniform and the seam is wired; Populate is a no-op.
    [UsedImplicitly]
    public sealed class DistrictBuildActionsUISubSystem : DistrictBuildUISubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.Actions;

        public DistrictBuildActionsUISubSystem(EntityStore world) : base(world)
        {
        }

        public override void Populate(GameObject root)
        {
        }
    }
}
