using Domains.Economy.District.Components;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictBuildOutcome.Components;
using Domains.Economy.DistrictBuildOutcome.Tags;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Tags;
using Domains.Economy.Resource.Components;
using Domains.Map.Hex.Components;
using Friflo.Engine.ECS;

namespace Domains.Economy.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Domains.Economy assembly. Scope is the ASSEMBLY, not a
    ///     feature. Each method returns the live <see cref="Archetype" /> — entities are created BY it and
    ///     iterated THROUGH it; the caller keeps it in its own field. This holder stores nothing.
    /// </summary>
    public static class EconomyArchetypes
    {
        /// <summary>The district row: PK, the hex it stands on, its kind, its build state.</summary>
        public static Archetype District(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictIdComponent, HexIdFKComponent, DistrictTypeComponent,
                    DistrictBuildStateComponent>(),
                Tags.Get<DistrictTag>());

        /// <summary>
        ///     A "single open" rule row. Open conditions form TWO archetypes under one tag: this one and
        ///     <see cref="OpenConditionExist" /> differ by composition — the Exist rule carries a
        ///     required-district column this one has no use for — so they are separate archetypes, born
        ///     distinct and never converted into each other.
        /// </summary>
        public static Archetype OpenConditionSingle(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictTypeFKComponent, DistrictOpenConditionKindComponent,
                    DistrictOpenStateComponent>(),
                Tags.Get<DistrictOpenConditionTag>());

        /// <summary>An "exists" rule row — carries the district it requires.</summary>
        public static Archetype OpenConditionExist(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictTypeFKComponent, DistrictExistConditionComponent,
                    DistrictOpenConditionKindComponent, DistrictOpenStateComponent>(),
                Tags.Get<DistrictOpenConditionTag>());

        /// <summary>A build-outcome rule row. The outcome KIND is a column, not a second archetype.</summary>
        public static Archetype BuildOutcome(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictTypeFKComponent, DistrictBuildOutcomeKindComponent>(),
                Tags.Get<DistrictBuildOutcomeTag>());

        /// <summary>
        ///     An owner-keyed resource row. Resource rows are owner-agnostic by design: only the owner FK
        ///     type and the owner's resource tag vary, so the archetype is declared generically here — in
        ///     the assembly that owns <see cref="ResourceComponent" /> — and closed by the calling assembly
        ///     (Domains.Actors supplies the City and Mayor pairs).
        /// </summary>
        public static Archetype Resource<TOwnerFK, TResourceTag>(EntityStore store)
            where TOwnerFK : struct, IComponent
            where TResourceTag : struct, ITag =>
            store.GetArchetype(
                ComponentTypes.Get<TOwnerFK, ResourceComponent>(),
                Tags.Get<TResourceTag>());
    }
}
