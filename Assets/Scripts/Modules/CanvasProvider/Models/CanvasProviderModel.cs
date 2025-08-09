using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Modules.CanvasProvider.Models
{
    [UsedImplicitly]
    public class CanvasProviderModel : ICanvasProviderModel
    {
        public GameObject RootGO { get; }
        public RectTransform RootTransform { get; }

        [Inject]
        public CanvasProviderModel(GameObject rootGo)
        {
            Debug.Log($"[skh] CanvasProviderModel.ctor({rootGo})");
            RootGO = rootGo;
            RootTransform = RootGO.GetComponent<RectTransform>();
        }
    }
}
