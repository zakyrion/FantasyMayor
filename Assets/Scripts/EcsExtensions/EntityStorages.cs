using Friflo.Engine.ECS;

namespace EcsExtensions
{
    public sealed class EntityStorages
    {
        public EntityStore World { get; }
        public SingletonStorage Singletons { get; }

        public EntityStorages()
        {
            World = new EntityStore();
            Singletons = new SingletonStorage();
        }
    }
}
