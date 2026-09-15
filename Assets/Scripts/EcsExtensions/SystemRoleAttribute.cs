using System;

namespace EcsExtensions
{
    // The role a system's base does not decide: an Update-loop class that holds an event archetype outside
    // base(...). MarkerShapeAnalyzer fails the compilation when the class's shape contradicts the role.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemRoleAttribute : Attribute
    {
        public SystemRoleKind Role { get; }

        public SystemRoleAttribute(SystemRoleKind role)
        {
            Role = role;
        }
    }
}
