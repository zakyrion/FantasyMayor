using UnityEngine;

namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Transient ECS component carrying the generated terrain texture.
    ///     Created by <see cref="Systems.TerrainViewTextureSubSystem" />, consumed and destroyed
    ///     by <see cref="Systems.TerrainViewSystem" /> which applies it to the mesh material.
    /// </summary>
    public struct TerrainTextureComponent
    {
        public Texture2D Texture;
    }
}
