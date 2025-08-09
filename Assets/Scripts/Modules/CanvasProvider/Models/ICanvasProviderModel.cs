using UnityEngine;

namespace Modules.CanvasProvider.Models
{
    public interface ICanvasProviderModel
    {
        GameObject RootGO { get; }
        RectTransform RootTransform { get; }
    }
}
