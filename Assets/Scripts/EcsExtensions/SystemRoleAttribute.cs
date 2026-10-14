using System;

namespace EcsExtensions
{
    // The role a system's base does not decide: a class that runs the Update loop and still holds a
    // readonly EventReader<TEvent> field yet ticks every frame rather than reacting. MarkerShapeAnalyzer
    // fails the compilation when the class's shape contradicts the role.
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
