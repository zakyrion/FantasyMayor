using EcsExtensions;
using JetBrains.Annotations;
using Domains.Map.HexResources.Data;

namespace Presentation.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class FishHexResourceViewSubSystem : HexResourcesViewSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Fish;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Fish;

        public FishHexResourceViewSubSystem(EntityStorages storages)
            : base(storages.World)
        {
        }

        public override void Update(GameState state)
        {
        }
    }
}
