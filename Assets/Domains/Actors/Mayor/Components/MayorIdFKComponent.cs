using Friflo.Engine.ECS;

namespace Domains.Actors.Mayor.Components
{
    // FK into the Mayor key space: carried by rows of other tables, keys their ComponentIndex.
    public struct MayorIdFKComponent : IIndexedComponent<int>
    {
        public int Value;

        public int GetIndexedValue() => Value;
    }
}
