using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.HexResources.Data;

namespace Modules.HexResourcesView.Systems
{
    [UsedImplicitly]
    internal sealed class FishResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private const int ExecutionPriority = 300;

        public override int Priority => ExecutionPriority;
        protected override ResourceType TargetResourceType => ResourceType.Fish;

        public FishResourceViewSubSystem(World world)
            : base(world)
        {
        }

        public override void Update(GameState state)
        {
        }
    }
}
