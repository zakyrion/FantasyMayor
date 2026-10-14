using System;

namespace EcsExtensions
{
    // A label tag: a role or membership marker beside an archetype's main tag. Never a query filter.
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class TagLabelAttribute : Attribute
    {
        public TagLabelRole Role { get; }

        public TagLabelAttribute()
        {
            Role = TagLabelRole.None;
        }

        public TagLabelAttribute(TagLabelRole role)
        {
            Role = role;
        }
    }
}
