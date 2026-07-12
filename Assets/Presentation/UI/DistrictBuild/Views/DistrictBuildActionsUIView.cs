using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    // ДІЇ section (dormant scaffold): the seam for per-action rows once the district-action model lands. No
    // content yet — exists so the section family (view + component + subsystem) is uniform. Validates its
    // PanelRenderer at init for parity with the other sections.
    public sealed class DistrictBuildActionsUIView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _renderer;

        private void OnEnable()
        {
            if (_renderer == null)
                throw new InvalidOperationException(
                    "DistrictBuildActionsUIView: PanelRenderer is not assigned on the prefab.");
        }
    }
}
