using System.Threading;
using Core;
using JetBrains.Annotations;
using Modules.Addressable;
using UnityEngine;
using Zenject;

namespace Modules.Hexes.Creators
{
    [UsedImplicitly]
    internal class HexesCreator : IHexesCreator
    {
        private IAddressable _addressable;
        private GameObject _root;

        public HexesCreator(IAddressable addressable, [Inject(Id = Constants.HEXES_ROOT_ID)] GameObject root)
        {
            _addressable = addressable;
            _root = root;
        }

        public Box<HexView> CreateHexAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
