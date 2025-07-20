using System.Threading;
using Core.Data;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Atoms.TerrainGeneration.UI
{
    public class HightmapUI : MonoBehaviour, IInitializable
    {
        [SerializeField] private RawImage _heightmapImage;
        [Inject] private IDataContainer<HeightmapDataLayer> _heightmapDataLayer;
        private SubscriptionId _subscriptionId;

        private void OnDestroy()
        {
            _heightmapDataLayer.UnsubscribeOnUpdate(_subscriptionId);
        }

        private UniTask HeightmapChangedAsync(HeightmapDataLayer dataLayer, CancellationToken cancellationToken)
        {
            _heightmapImage.gameObject.SetActive(true);
            _heightmapImage.texture = dataLayer.Texture;
            return UniTask.CompletedTask;
        }

        public void Initialize()
        {
            _subscriptionId = _heightmapDataLayer.SubscribeOnUpdate(HeightmapChangedAsync, 0).SubscriptionId;

            _heightmapImage.gameObject.SetActive(false);
        }
    }
}
