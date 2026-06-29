using System;
using Domains.Map.HexResources.Data;
using UnityEngine;

namespace Presentation.HexResources.Data
{
    [Serializable]
    public struct HexResourcesViewConfigEntry
    {
        public HexResourceType Type;
        public GameObject Prefab;
        public Vector2 ScaleRange;
        public float Radius;

        /// <summary>Color splatted onto the terrain texture under each placed instance; alpha drives strength.</summary>
        public Color GroundTint;
    }
}
