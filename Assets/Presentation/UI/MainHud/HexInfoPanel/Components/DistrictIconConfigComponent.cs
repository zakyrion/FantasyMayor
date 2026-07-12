using Core;
using Presentation.UI.MainHud.HexInfoPanel.Configs;

namespace Presentation.UI.MainHud.HexInfoPanel.Components
{
    /// <summary>
    ///     World component wrapping the loaded district-icon config. Holds the <see cref="Box{T}" /> so the
    ///     sprite assets stay loaded for the panel's lifetime (ownership is transferred here by the loader).
    /// </summary>
    public readonly struct DistrictIconConfigComponent
    {
        private readonly Box<DistrictIconConfig> _config;

        public DistrictIconConfig Value => _config.Value;

        public DistrictIconConfigComponent(Box<DistrictIconConfig> config)
        {
            _config = config;
        }
    }
}
