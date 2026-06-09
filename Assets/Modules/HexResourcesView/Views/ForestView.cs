using UnityEngine;

namespace Modules.HexResourcesView.Views
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
