namespace Domains.Economy.District.Components
{
    // World component: monotonic source of DistrictId values. `Next` is the id the next District will take.
    // Persisted across save/load (contract only for now — ES3 wiring deferred).
    public struct DistrictIdAllocatorComponent
    {
        public int Next;
    }
}
