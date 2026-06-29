using Modules.AxialSystem;
using Domains.Map.Hex.Utils;
using Unity.Collections;
using Unity.Mathematics;

namespace Presentation.HexResources.Helpers
{
    /// <summary>
    ///     Sinks the shared <see cref="VertexGrid" /> into a clay bowl for one hex. Only the hex's own
    ///     vertices that fall inside the <see cref="ClayFootprint" /> are lowered, with a cosine falloff
    ///     that reaches zero at the contour edge — so boundary vertices shared with neighbours are not
    ///     pulled down and no seam forms (provided the footprint stays within the hex inradius).
    ///     Like the isoline depressions, this only ever lowers a vertex, never raises it.
    /// </summary>
    internal sealed class ClayDepressionShaper
    {
        public void Shape(VertexGrid grid, HexCoord hex, in ClayFootprint footprint, float2 centerXZ, float depth)
        {
            if (depth <= 0f)
                return;

            // Snapshot the owned coords first: GetOwnedVertexCoords returns the live owner-cache set, and
            // grid.Set() mutates that same set — enumerating it directly throws "Collection was modified".
            var owned = new NativeList<VertexCoord>(32, Allocator.Temp);
            foreach (var coord in grid.GetOwnedVertexCoords(hex))
                owned.Add(coord);

            for (var i = 0; i < owned.Length; i++)
            {
                var coord = owned[i];
                var vertex = grid.Get(coord);
                var pointXZ = new float2(vertex.Position.x, vertex.Position.z);

                if (!footprint.Evaluate(centerXZ, pointXZ, out var t))
                    continue;

                // Cosine bowl: full depth at the center, smoothly to zero at the edge (C1 at the rim).
                var fall = 0.5f * (1f + math.cos(math.PI * t));
                vertex.Position.y -= depth * fall;
                grid.Set(coord, vertex);
            }

            owned.Dispose();
        }
    }
}
