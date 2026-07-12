using System;
using Domains.Economy.District.Data;

namespace Domains.Economy.District.Helpers
{
    // Shared "find an authored config entry by DistrictType" scan — was duplicated verbatim across
    // DistrictBuildPriceUISubSystem, DistrictBuildHexResourcesUISubSystem, BuildDistrictActionSystem,
    // and DistrictBuildProgressViewSpawnSystem (see Flows/FLOW_DISTRICT_BUILD.md dedup-debt). Generic over the
    // element type since each caller scans a different authored array (DistrictBuildCostConfig,
    // DistrictBuildConfig, DistrictBuildProgressViewConfig); keySelector/extraPredicate are non-capturing
    // lambdas at every call site, so the compiler caches them — no per-call allocation.
    public static class DistrictConfigLookup
    {
        public static bool TryFind<T>(T[] entries, DistrictType type, Func<T, DistrictType> keySelector, out T match)
            where T : class
            => TryFind(entries, type, keySelector, null, out match);

        public static bool TryFind<T>(T[] entries, DistrictType type, Func<T, DistrictType> keySelector,
            Func<T, bool> extraPredicate, out T match) where T : class
        {
            if (entries != null)
                foreach (var entry in entries)
                    if (entry != null && keySelector(entry) == type && (extraPredicate == null || extraPredicate(entry)))
                    {
                        match = entry;
                        return true;
                    }

            match = null;
            return false;
        }
    }
}
