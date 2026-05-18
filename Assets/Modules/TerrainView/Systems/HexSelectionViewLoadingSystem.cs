using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.TerrainGenerator.Components;
using Modules.TerrainView.Components;
using Modules.TerrainView.Views;

namespace Modules.TerrainView.Systems
{
    /// <summary>
    ///     Loads the addressable <see cref="HexSelectionView" /> prefab and publishes
    ///     the runtime singleton view entity.
     ///     Owns the <c>Box&lt;HexSelectionView&gt;</c>; disposal destroys the instantiated carrier object.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexSelectionViewLoadingSystem : UpdatedSystem
    {
        internal const int ExecutionPriority = TerrainViewSystem.ExecutionPriority + 1;
        private const string HEX_SELECTION_VIEW_ADDRESS = "HexSelectionView";

        private readonly IAddressable _addressable;
        private readonly World _world;

        private CancellationTokenSource _cts;
        private Entity? _hexSelectionViewEntity;
        private Box<HexSelectionView> _hexSelectionViewBox;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        public HexSelectionViewLoadingSystem(World world, IAddressable addressable)
            : base(world.GetEntities()
                .WhenAdded<TerrainGenerationGenerateEventComponent>()
                .AsSet())
        {
            _world = world;
            _addressable = addressable;
            _hexSelectionViewBox = Box<HexSelectionView>.Empty();
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            LoadViewAsync(CancellationTokenSource.CreateLinkedTokenSource(StatusMonitor.Token, _cts.Token).Token).Forget();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DestroyHexSelectionViewEntity();
            DisposeHexSelectionViewBox();
            base.Dispose();
        }

        private void DestroyHexSelectionViewEntity()
        {
            if (_hexSelectionViewEntity == null || !_hexSelectionViewEntity.Value.IsAlive)
                return;

            _hexSelectionViewEntity.Value.Dispose();
            _hexSelectionViewEntity = null;
        }

        private void DisposeHexSelectionViewBox()
        {
            if (!_hexSelectionViewBox.Exist)
                return;

            _hexSelectionViewBox.Dispose();
            _hexSelectionViewBox = Box<HexSelectionView>.Empty();
        }

        private async UniTask LoadViewAsync(CancellationToken cancellationToken)
        {
            DestroyHexSelectionViewEntity();
            DisposeHexSelectionViewBox();

            var result = await _addressable.LoadAndInstanceAsync<HexSelectionView>(HEX_SELECTION_VIEW_ADDRESS, cancellationToken);
            if (cancellationToken.IsCancellationRequested || result.Status != Status.Success)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            _hexSelectionViewBox = result.Box;
            var view = _hexSelectionViewBox.Value;
            view.HideSelectionMesh();

            var entity = _world.CreateEntity();
            entity.Set(new HexSelectionViewComponent { ObjectRef = view });
            _hexSelectionViewEntity = entity;
        }
    }
}
