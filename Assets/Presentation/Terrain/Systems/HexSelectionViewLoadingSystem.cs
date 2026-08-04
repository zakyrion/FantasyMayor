using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Presentation.Terrain.Views;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 500). Loads the addressable <see cref="HexSelectionView" /> prefab
    ///     and publishes the runtime singleton view entity. Runs after the terrain view exists.
    ///     Owns the <c>Box&lt;HexSelectionView&gt;</c>; disposal destroys the instantiated carrier object.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexSelectionViewLoadingSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const string HEX_SELECTION_VIEW_ADDRESS = "HexSelectionView";

        private readonly IAddressable _addressable;
        private readonly EntityStore _world;
        private readonly Archetype _archetype;

        private Box<HexSelectionView> _hexSelectionViewBox;
        private Entity? _hexSelectionViewEntity;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.HexSelectionViewLoading;

        public HexSelectionViewLoadingSystem(EntityStore world, IAddressable addressable)
        {
            _world = world;
            _addressable = addressable;
            _hexSelectionViewBox = Box<HexSelectionView>.Empty();
            _archetype = PresentationArchetypes.HexSelectionView(world);
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            return LoadViewAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DestroyHexSelectionViewEntity();
            DisposeHexSelectionViewBox();
        }

        private void DestroyHexSelectionViewEntity()
        {
            if (_hexSelectionViewEntity == null || _hexSelectionViewEntity.Value.IsNull)
                return;

            _hexSelectionViewEntity.Value.DeleteEntity();
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

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new HexSelectionViewComponent { ObjectRef = view });
            _hexSelectionViewEntity = entity;
        }
    }
}
