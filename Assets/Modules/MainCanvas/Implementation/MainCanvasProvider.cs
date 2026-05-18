using JetBrains.Annotations;
using Modules.MainCanvas.Core;
using UnityEngine;

namespace Modules.MainCanvas.Implementation
{
    [UsedImplicitly]
    public class MainCanvasProvider : IMainCanvasProvider
    {
        public GameObject RootGO { get; }
        public Transform RootTransform { get; }

        public MainCanvasProvider(GameObject rootGo)
        {
            RootGO = rootGo;
            RootTransform = RootGO.GetComponent<Transform>();
        }
    }
}
