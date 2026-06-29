using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Domains.Map.HexResources.Data;

namespace Presentation.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class FishHexResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private const int ExecutionPriority = 300;

        public override int Priority => ExecutionPriority;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Fish;

        public FishHexResourceViewSubSystem(World world)
            : base(world)
        {
        }

        public override void Update(GameState state)
        {
        }
    }
}
