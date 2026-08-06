using Friflo.Engine.ECS;
using UnityEngine;

namespace Presentation.Terrain.Components
{
    /// <summary>
    ///     Persistent <b>singleton component</b> carrying the generated terrain texture (set via
    ///     <c>Singletons.Set</c>, read via <c>Singletons.Get</c> — not attached to any hex entity).
    ///     Created by <see cref="Systems.TerrainViewTextureSubSystem" /> and applied to the mesh material
    ///     by <see cref="Systems.TerrainViewSystem" />. It persists past world-init so that reactive
    ///     runtime systems (e.g. forest ground painting) can mutate the same <see cref="Texture2D" />
    ///     instance; mutations are reflected by the material automatically.
    /// </summary>
    public struct TerrainTextureComponent : IComponent
    {
        public Texture2D Texture;
    }
}
