using System;

namespace Core
{
    // Marks a system instance field as a deliberately-allowed exception to the "no stateful systems" ban.
    // The /arch-check scanner skips fields bearing this attribute. Use it only for fields that genuinely
    // must persist on the instance (e.g. a per-frame cache resolved in PreUpdate); pass a short reason.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StateAllowedAttribute : Attribute
    {
        public string Reason { get; }

        public StateAllowedAttribute(string reason = null)
        {
            Reason = reason;
        }
    }
}
