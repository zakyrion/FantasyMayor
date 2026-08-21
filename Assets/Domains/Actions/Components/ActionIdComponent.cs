using Friflo.Engine.ECS;
using System;

namespace Domains.Actions.Components
{
    // Primary key of an action row (the shared action key space — keeps the singular `Action` concept prefix,
    // not the domain folder). IEquatable so any table can key/join on it. The creating system allocates the
    // Value (Step-4 concern); this defines only the type. Key-role law: a PK type, never shared as another
    // table's FK (ARCHITECTURE.md → key-role-law).
    public struct ActionIdComponent : IEquatable<ActionIdComponent>, IComponent
    {
        public int Value;

        public bool Equals(ActionIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ActionIdComponent other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }
}
