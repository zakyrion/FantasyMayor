using System;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.Binders
{
    /// <summary>Which actor pays the resource cost (the district owner). AP is always Mayor-paid.</summary>
    internal enum Payer
    {
        Mayor,
        City
    }

    /// <summary>
    ///     ПЛАТНИК section. Owns the two static payer segments (Мер / Місто); a click reports the chosen payer
    ///     through <c>onSelect</c> and the view re-binds the active highlight + the cost "have" column.
    /// </summary>
    internal sealed class DistrictPayerBinder
    {
        private readonly VisualElement _mayorSegment;
        private readonly VisualElement _citySegment;

        public DistrictPayerBinder(VisualElement mayorSegment, VisualElement citySegment, Action<Payer> onSelect)
        {
            _mayorSegment = mayorSegment;
            _citySegment = citySegment;
            _mayorSegment.RegisterCallback<ClickEvent>(_ => onSelect(Payer.Mayor));
            _citySegment.RegisterCallback<ClickEvent>(_ => onSelect(Payer.City));
        }

        public void Bind(Payer active)
        {
            _mayorSegment.EnableInClassList("payseg--on", active == Payer.Mayor);
            _citySegment.EnableInClassList("payseg--on", active == Payer.City);
        }
    }
}
