using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.Resources.Components;
using Presentation.Resources.Configs;

namespace Presentation.Resources.Systems
{
    [UsedImplicitly]
    internal sealed class ClayViewConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string CLAY_VIEW_CONFIG = "ClayViewConfig";

        public ClayViewConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var loadedConfig = Box<ClayViewConfig>.Empty();

            try
            {
                loadedConfig = await LoadConfigAsync<ClayViewConfig>(CLAY_VIEW_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !loadedConfig.Exist)
                    return;

                // Flattened component owns its data — the ScriptableObject is not retained past load.
                World.Set(ClayViewConfigComponent.FromConfig(loadedConfig.Value));
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref loadedConfig);
            }
        }
    }
}
