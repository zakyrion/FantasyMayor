using UnityEngine;

namespace Presentation.Resources.Views
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
