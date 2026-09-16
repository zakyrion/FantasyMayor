using EcsExtensions;
namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     Event: at least one in-progress district build's countdown reached zero this turn.
    ///     <c>BuildDistrictTurnTickSystem</c> raises it once per turn, only when a counter first reaches zero;
    ///     <c>BuildDistrictCompletionSystem</c> reconciles every in-progress build against state rather than
    ///     trusting the count of deliveries. Public — its <see cref="EventReader{TEvent}" /> is statically
    ///     mentioned in <c>EventReaderAotDeclarations</c>, which lives outside this assembly.
    /// </summary>
    public struct BuildDistrictCompleteEvent : IEventTag
    {
    }
}
