using Friflo.Engine.ECS;
namespace Domains.Map.Generation.Components
{
    /// <summary>Stores sea generation parameters extracted from <see cref="Configs.SeaConfig"/>.</summary>
    public struct SeaConfigComponent : IComponent
    {
        /// <summary>Sea area as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction;
    }
}
