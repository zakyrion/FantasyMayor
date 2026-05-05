using System;
using Unity.Mathematics;

namespace Modules.AxialSystem
{
    public interface IAxialCoord<TSelf> : IEquatable<TSelf> where TSelf : struct
    {
        int2 Value { get; }
    }
}
