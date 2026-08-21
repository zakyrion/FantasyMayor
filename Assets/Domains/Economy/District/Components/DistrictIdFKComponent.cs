using Friflo.Engine.ECS;

namespace Domains.Economy.District.Components
{
    // FK into the District PK space: carried by rows of other tables, keys their ComponentIndex.
    public struct DistrictIdFKComponent : IIndexedComponent<int>
    {
        public int Value;

        public int GetIndexedValue() => Value;
    }
}
