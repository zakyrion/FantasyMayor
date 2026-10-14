using System.Threading;
using Cysharp.Threading.Tasks;
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
            : base(storages)
        {
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
