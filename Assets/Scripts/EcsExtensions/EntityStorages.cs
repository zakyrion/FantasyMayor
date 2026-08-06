using Friflo.Engine.ECS;

namespace EcsExtensions
{
    public sealed class EntityStorages
    {
        public EntityStore World { get; }
        public SingletonComponents Singletons { get; }

        public EntityStorages(in SingletonArchetypeDefinition singletonArchetype)
        {
            World = new EntityStore();
            Singletons = new SingletonComponents(singletonArchetype);
        }
    }
}
