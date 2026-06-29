using Core;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Components
{
    /// <summary>
    ///     World component holding the addressable Box for the district-build overlay's own UIDocument root
    ///     (a separate document from the shared Main UI, with a higher sort order). DistrictBuildUISpawnSystem
    ///     owns the handle and disposes it on teardown. Mirrors MainUIComponent.
    /// </summary>
    public struct DistrictBuildUIRootComponent
    {
        public Box<GameObject> RootBox;
    }
}
