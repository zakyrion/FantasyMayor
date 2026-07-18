using Friflo.Engine.ECS;
namespace Modules.Turn.Components
{
    /// <summary>
    ///     World singleton holding the current turn number. Seeded to 1 when Gameplay starts (the first Mayor
    ///     Phase is turn 1) and incremented by <c>TurnCountSystem</c> after each turn fully resolves. Read by the
    ///     MainUI turn cluster to show "Хід N". Immutable — write a new value via <c>World.Set</c>.
    /// </summary>
    public readonly struct TurnCountComponent : IComponent
    {
        public readonly int Value;

        public TurnCountComponent(int value)
        {
            Value = value;
        }
    }
}
