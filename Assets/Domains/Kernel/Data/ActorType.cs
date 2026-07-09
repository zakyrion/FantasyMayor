using System;

namespace Domains.Kernel.Data
{
    // Shared-kernel vocabulary token: the ONE owner-type enum both Economy (owner-affinity masks) and
    // Actors (actor identity) may reference without depending on each other. The kernel stays minimal by
    // design — vocabulary only, no logic/components/engine deps; owner FK components (CityIdComponent,
    // MayorIdComponent) are actor-owned identity and deliberately stay in Actors. [Flags] so callers can
    // express an allowed-owner MASK (e.g. Mayor | City), not just a single kind.
    [Flags]
    public enum ActorType
    {
        Unknown = 0,
        Mayor = 1,
        City = 2
    }
}
