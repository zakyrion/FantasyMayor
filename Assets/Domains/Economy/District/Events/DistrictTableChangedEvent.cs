using Domains.Economy.District.Data;
using Friflo.Engine.ECS;
using System;

namespace Domains.Economy.District.Events
{
    /// <summary>
    ///     One-frame event: a District row entered <c>Change</c>. DEVIATES from PATTERN_EVENT's payload-less
    ///     default BY DECISION (FLOW_DISTRICT_BUILD, 2026-07-17): <c>Change</c> is a DECIDED filter key, not a
    ///     payload copy — a <c>Removed</c> row no longer exists to read its stage from, so the column cannot
    ///     express it. IEquatable so consumers filter via <c>AsMultiMap&lt;DistrictTableChangedEvent&gt;</c> keyed
    ///     by value; reconcile stays global + idempotent per kind, never trusting single delivery. Folds
    ///     <c>DistrictBuiltEvent</c> (Built is now one of three kinds this event covers). Raised by
    ///     <c>Domains.Actions.BuildDistrictAction</c> systems (legal: Actions → Economy) on the pulse's own entity
    ///     alongside <c>EventTag</c>. Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictTableChangedEvent : IEquatable<DistrictTableChangedEvent>, IComponent
    {
        public DistrictTableChange Change;

        public bool Equals(DistrictTableChangedEvent other) => Change == other.Change;
        public override bool Equals(object obj) => obj is DistrictTableChangedEvent other && Equals(other);
        public override int GetHashCode() => (int)Change;
    }
}
