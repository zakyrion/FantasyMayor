using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Domains.Map.HexResources.Data;

namespace Presentation.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class FishHexResourceViewSubSystem : HexResourcesViewSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Fish;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Fish;

        public FishHexResourceViewSubSystem(EntityStore world)
            : base(world)
        {
        }

        public override void Update(GameState state)
        {
        }
    }
}
