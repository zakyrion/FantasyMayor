using UnityEngine;

namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Persistent <b>world component</b> carrying the generated terrain texture (set via
    ///     <c>World.Set</c>, read via <c>World.Get</c> — not attached to any hex entity).
    ///     Created by <see cref="Systems.TerrainViewTextureSubSystem" /> and applied to the mesh material
    ///     by <see cref="Systems.TerrainViewSystem" />. It persists past world-init so that reactive
    ///     runtime systems (e.g. forest ground painting) can mutate the same <see cref="Texture2D" />
    ///     instance; mutations are reflected by the material automatically.
    /// </summary>
    public struct TerrainTextureComponent
    {
        public Texture2D Texture;
    }
}
