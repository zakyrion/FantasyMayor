using UnityEngine;

namespace Modules.MainCanvas.Core
{
    public interface IMainCanvasProvider
    {
        GameObject RootGO { get; }
        Transform RootTransform { get; }
    }
}
