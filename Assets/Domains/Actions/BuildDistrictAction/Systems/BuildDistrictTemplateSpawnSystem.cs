using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildStartedEvent" /> pulse spawns the draft build entity —
    ///     <c>HexIdComponent</c> (which hex) + <c>DistrictTypeComponent</c> (chosen district, Unknown until the
    ///     player picks one) + <c>BuildDistrictActionTemplateTag</c>. Hex/district come from the pulse payload, not
    ///     world state (the Actions assembly can't read the Presentation selection). One overlay is open at a time,
    ///     so the pulse fires once per open. See <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictTemplateSpawnSystem : UpdatedSystem
    {
        private readonly World _world;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictTemplateSpawn;

        public BuildDistrictTemplateSpawnSystem(World world)
            : base(world.GetEntities().With<DistrictBuildStartedEvent>().AsSet())
        {
            _world = world;
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var started = pulse.Get<DistrictBuildStartedEvent>();

            var entity = _world.CreateEntity();
            entity.Set(new HexIdComponent { Coords = started.Coords });
            entity.Set(new DistrictTypeComponent { Value = started.Type });
            entity.Set(new BuildDistrictActionTemplateTag());
        }
    }
}
