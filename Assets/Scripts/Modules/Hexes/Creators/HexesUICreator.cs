using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Modules.Addressable;
using Modules.CanvasProvider.Models;
using Modules.Hexes.View.UI;

namespace Modules.Hexes.Creators
{
    public class HexesUICreator : IHexesUICreator
    {
        private const string HEXES_UI_PREFAB = "UI/HexUI";

        private readonly IAddressable _addressable;
        private readonly ICanvasProviderModel _canvasProvider;

        public HexesUICreator(ICanvasProviderModel canvasProvider, IAddressable addressable)
        {
            _canvasProvider = canvasProvider;
            _addressable = addressable;
        }

        public async UniTask<Box<HexesUIView>> CreateHexesUIAsync(CancellationToken cancellationToken)
        {
            var result = await _addressable.LoadAndInstanceAsync(HEXES_UI_PREFAB, cancellationToken, _canvasProvider.RootTransform);
            if (cancellationToken.IsCancellationRequested)
            {
                result.Box.Dispose();
                return Box<HexesUIView>.Empty();
            }

            if (result.Status == AddressableStatus.Success)
            {
                var component = result.Box.Value.GetComponent<HexesUIView>();

                if (component == null)
                    return Box<HexesUIView>.Empty();

                return Box<HexesUIView>.Wrap(component, _ => result.Box.Dispose());
            }

            return Box<HexesUIView>.Empty();
        }
    }
}
