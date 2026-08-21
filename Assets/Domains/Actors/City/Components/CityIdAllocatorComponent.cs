using Friflo.Engine.ECS;
namespace Domains.Actors.City.Components
{
    // Singleton component: monotonic source of CityId values. `Next` is the id the next City will take.
    // Persisted across save/load (contract only for now — ES3 wiring deferred).
    public struct CityIdAllocatorComponent : IComponent
    {
        public int Next;
    }
}
