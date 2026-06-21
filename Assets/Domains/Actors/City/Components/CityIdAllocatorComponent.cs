namespace Domains.Actors.City.Components
{
    // World component: monotonic source of CityId values. `Next` is the id the next City will take.
    // Persisted across save/load (contract only for now — ES3 wiring deferred).
    public struct CityIdAllocatorComponent
    {
        public int Next;
    }
}
