using Domains.Map.HexResources.Data;
using Friflo.Engine.ECS;
using Presentation.HexResources.Views;

namespace Presentation.HexResources.Components
{
    /// <summary>
    ///     One spawned tree view: the resource type and the view MonoBehaviour reference. The reference is
    ///     held so the <see cref="ForestView" /> GameObject can be destroyed when the hex stops being a forest.
    ///     Forest ground paint is append-only and never reverted, so no splat data is stored here.
    /// </summary>
    internal struct ForestViewComponent : IComponent
    {
        public HexResourceType Type;
        public ForestView View;
    }
}
