using Friflo.Engine.ECS;

namespace EcsExtensions
{
    public readonly struct SingletonArchetypeDefinition
    {
        public readonly ComponentTypes ComponentTypes;
        public readonly Tags Tags;

        public SingletonArchetypeDefinition(in ComponentTypes componentTypes, in Tags tags)
        {
            ComponentTypes = componentTypes;
            Tags = tags;
        }
    }
}
