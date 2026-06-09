using UnityEngine;

namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Persistent ECS component carrying the generated terrain texture.
    ///     Created by <see cref="Systems.TerrainViewTextureSubSystem" /> and applied to the mesh material
    ///     by <see cref="Systems.TerrainViewSystem" />. The entity is kept alive past world-init so that
    ///     reactive runtime systems (e.g. forest ground painting) can mutate the same
    ///     <see cref="Texture2D" /> instance; mutations are reflected by the material automatically.
    /// </summary>
    public struct TerrainTextureComponent
    {
        public Texture2D Texture;
    }
}
