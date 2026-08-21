using Friflo.Engine.ECS;

namespace Domains.Economy.District.Components
{
    // Primary key of a District entity. Key-role law: a PK type — any other table scoped to a
    // district carries a dedicated …FKComponent, never this type directly (ARCHITECTURE.md → key-role-law).
    public struct DistrictIdComponent : IIndexedComponent<int>
    {
        public int Value;

        public int GetIndexedValue() => Value;
    }
}
