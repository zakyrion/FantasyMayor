using UnityEngine;

namespace Presentation.Districts.Views
{
    [DisallowMultipleComponent]
    public sealed class DistrictView : MonoBehaviour
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
