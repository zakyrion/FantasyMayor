using System;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>Which actor pays the resource cost (the district owner). AP is always Mayor-paid.</summary>
    public enum Payer
    {
        Mayor,
        City
    }

    /// <summary>
    ///     ПЛАТНИК panel. Owns the payer toggle state and the two static segments (Мер / Місто): a click switches
    ///     the active payer, refreshes the highlight, and raises <see cref="PayerChanged" /> so the coordinator
    ///     re-binds the cost column. <see cref="Current" /> is the canonical payer (no copy on the coordinator).
    /// </summary>
    internal sealed class DistrictPayerSubView
    {
        private readonly VisualElement _mayorSegment;
        private readonly VisualElement _citySegment;
        private Payer _payer;

        public event Action<Payer> PayerChanged;

        public DistrictPayerSubView(VisualElement mayorSegment, VisualElement citySegment)
        {
            _mayorSegment = mayorSegment;
            _citySegment = citySegment;
            _mayorSegment.RegisterCallback<ClickEvent>(_ => Select(Payer.Mayor));
            _citySegment.RegisterCallback<ClickEvent>(_ => Select(Payer.City));
        }

        public Payer Current => _payer;

        public void Bind() => Refresh();

        private void Select(Payer payer)
        {
            if (_payer == payer)
                return;

            _payer = payer;
            Refresh();
            PayerChanged?.Invoke(_payer);
        }

        private void Refresh()
        {
            _mayorSegment.EnableInClassList("payseg--on", _payer == Payer.Mayor);
            _citySegment.EnableInClassList("payseg--on", _payer == Payer.City);
        }
    }
}
