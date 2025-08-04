using System;
using Core.Data;
using Core.DataLayer;
using JetBrains.Annotations;
using Modules.Hexes.Creators;

namespace Modules.Hexes.ViewManagers
{
    [UsedImplicitly]
    internal class HexesViewManager : IDisposable
    {
        private readonly IHexesCreator _hexesCreator;
        private readonly DataSubscribeResult _hexesDataLayerSubscription;

        public HexesViewManager(IDataHub dataHub, IHexesCreator hexesCreator)
        {
            _hexesCreator = hexesCreator;
        }

        public void Dispose()
        {

        }
    }
}
