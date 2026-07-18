using System;

namespace EcsExtensions
{
    /// <summary>
    ///     A system driven by an explicit external caller (an orchestrator), as opposed to
    ///     <see cref="IUpdatedSystem" />/<see cref="ILateUpdatedSystem" /> which the Unity loop drives directly.
    /// </summary>
    /// <typeparam name="T">The type of the object used as state to update the system.</typeparam>
    public interface ISystem<in T> : IDisposable
    {
        /// <summary>Determines whether this system participates when its orchestrator runs.</summary>
        bool IsEnabled { get; set; }

        /// <summary>Updates the system once.</summary>
        void Update(T state);
    }
}
