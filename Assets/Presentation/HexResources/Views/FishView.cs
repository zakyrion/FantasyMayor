using UnityEngine;

namespace Presentation.HexResources.Views
{
    [DisallowMultipleComponent]
    public sealed class FishView : MonoBehaviour
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
