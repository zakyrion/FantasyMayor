using System;

namespace Domains.Kernel.Data
{
    [Flags]
    public enum ActorType
    {
        Unknown = 0,
        Mayor = 1,
        City = 2
    }
}
