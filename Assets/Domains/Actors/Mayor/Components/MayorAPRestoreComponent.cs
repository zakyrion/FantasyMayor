namespace Domains.Actors.Mayor.Components
{
    // Per-turn Action Point restore amount for the Mayor. NOT the live AP pool — the live pool is an
    // inventory resource stack (ResourceComponent of ResourceType.ActionPoint, owner FK MayorIdFKComponent).
    // The AP-restore turn phase resets that stack's Amount to this Value at the start of each new turn.
    public struct MayorAPRestoreComponent
    {
        public int Value;
    }
}
