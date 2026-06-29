using System.Collections.Generic;
using Modules.AxialSystem;

namespace Presentation.Terrain.Data
{
    public sealed record IsolineData
    {
        public float Distance { get; set; }
        public float Height { get; set; }
        public VertexCoord this[int index] => Vertices[index];
        public int VertexCount => Vertices.Count;
        public List<VertexCoord> Vertices { get; set; } = new();

        public float S(int index)
        {
            return index / Distance;
        }
    }
}
