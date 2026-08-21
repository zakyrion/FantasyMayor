using Friflo.Engine.ECS;
namespace Domains.Actors.Mayor.Components
{
    // Singleton component: source of MayorId values. The game has a single mayor, so `Next` effectively
    // stays at 1; kept for symmetry with CityIdAllocatorComponent and the save/load contract.
    public struct MayorIdAllocatorComponent : IComponent
    {
        public int Next;
    }
}
