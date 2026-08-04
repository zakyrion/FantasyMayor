using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Try-pattern accessors for the single-entity case: a query or <see cref="ComponentIndex{TComponent,TValue}" />
    ///     lookup expected to match at most one entity (an actor row, a PK row). Neither
    ///     <see cref="QueryEntities" /> nor <see cref="Entities" /> exposes an indexer, so the first match is taken
    ///     via a one-step foreach.
    /// </summary>
    public static class QueryResultExtensions
    {
        public static bool TryGetFirst(this ArchetypeQuery query, out Entity entity)
        {
            foreach (var candidate in query.Entities)
            {
                entity = candidate;
                return true;
            }

            entity = default;
            return false;
        }

        public static bool TryGetFirst(this Archetype archetype, out Entity entity)
        {
            foreach (var candidate in archetype.Entities)
            {
                entity = candidate;
                return true;
            }

            entity = default;
            return false;
        }

        public static bool TryGetFirst(this Entities entities, out Entity entity)
        {
            foreach (var candidate in entities)
            {
                entity = candidate;
                return true;
            }

            entity = default;
            return false;
        }
    }
}
