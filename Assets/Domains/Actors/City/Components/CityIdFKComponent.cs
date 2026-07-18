using Friflo.Engine.ECS;

namespace Domains.Actors.City.Components
{
    // FK into the City key space: carried by rows of other tables, keys their ComponentIndex.
    public struct CityIdFKComponent : IIndexedComponent<int>
    {
        public int Value;

        public int GetIndexedValue() => Value;
    }
}
