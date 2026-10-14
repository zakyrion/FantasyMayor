using System;
using Core;

namespace EcsExtensions
{
    /// <summary>
    ///     A reader's handle into the event log: the log itself and this reader's own cursor id. DI builds one
    ///     per injection (Transient) — each owner gets its own cursor, starting at "no offset yet". The field
    ///     itself is not state: the cursor lives in <see cref="EventLog" />. VContainer never disposes a
    ///     Transient, so the owner disposes its reader in its own Dispose, and the cursor goes back to the log.
    /// </summary>
    public sealed class EventReader<TEvent> : IDisposable where TEvent : struct, IEventTag
    {
        private readonly EventLog _log;
        private readonly int _cursorId;

        public EventReader(EntityStorages storages)
        {
            _log = storages.Events;
            _cursorId = _log.OpenCursor<TEvent>();
        }

        /// <summary>Hands the next unread event of this reader's type. False when none is left — the read is what marks it read.</summary>
        public bool TryRead(out TEvent evt) => _log.TryRead(_cursorId, out evt);

        /// <summary>Reads every event left unread and says whether the batch was non-empty — for a reaction that fires once per batch, not once per event.</summary>
        public bool DrainBatch()
        {
            var batchHeldEvents = false;

            while (TryRead(out _))
                batchHeldEvents = true;

            return batchHeldEvents;
        }

        /// <summary>Hands the cursor back to the log for the next reader; a reader disposed twice throws.</summary>
        public void Dispose() => _log.CloseCursor(_cursorId);
    }
}
