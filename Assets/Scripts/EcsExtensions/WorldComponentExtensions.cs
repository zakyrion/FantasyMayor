using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Read/write access to the single <c>"world"</c> singleton entity that carries every world-scoped
    ///     component (see PLAN_FRIFLO_MIGRATION.md → World components spec). The singleton is created once
    ///     in <c>WorldInstaller</c>, before any world-component write.
    /// </summary>
    public static class WorldComponentExtensions
    {
        private const string WorldEntityName = "world";

        public static T GetWorldComponent<T>(this EntityStore store) where T : struct, IComponent =>
            store.GetUniqueEntity(WorldEntityName).GetComponent<T>();

        public static bool HasWorldComponent<T>(this EntityStore store) where T : struct, IComponent =>
            store.GetUniqueEntity(WorldEntityName).HasComponent<T>();

        public static void SetWorldComponent<T>(this EntityStore store, in T component) where T : struct, IComponent =>
            store.GetUniqueEntity(WorldEntityName).AddComponent(component);

    }
}
