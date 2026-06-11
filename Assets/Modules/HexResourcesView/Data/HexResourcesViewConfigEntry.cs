using System;
using Modules.HexResources.Data;
using UnityEngine;

namespace Modules.HexResourcesView.Data
{
    [Serializable]
    public struct HexResourcesViewConfigEntry
    {
        public ResourceType Type;
        public GameObject Prefab;
        public Vector2 ScaleRange;
        public float Radius;

        /// <summary>Color splatted onto the terrain texture under each placed instance; alpha drives strength.</summary>
        public Color GroundTint;
    }
}
