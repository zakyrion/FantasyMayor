using Friflo.Engine.ECS;
namespace Domains.Actions.Components
{
    // World component: source of ActionId values. Seeded to 1 on first allocation and incremented per action
    // entity, so every live action row carries a unique ActionIdComponent PK. Mirrors MayorIdAllocatorComponent.
    public struct ActionIdAllocatorComponent : IComponent
    {
        public int Next;
    }
}
