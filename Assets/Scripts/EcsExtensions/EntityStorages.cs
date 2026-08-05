using Friflo.Engine.ECS;

namespace EcsExtensions
{
    public sealed class EntityStorages
    {
        public EntityStore World { get; }

        public EntityStorages()
        {
            World = new EntityStore();
        }
    }
}
