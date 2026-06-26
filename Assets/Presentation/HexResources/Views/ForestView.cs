using UnityEngine;

namespace Presentation.HexResources.Views
{
    [DisallowMultipleComponent]
    public sealed class ForestView : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
