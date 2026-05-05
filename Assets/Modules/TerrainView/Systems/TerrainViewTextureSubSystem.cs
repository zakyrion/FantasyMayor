using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;

namespace Modules.TerrainView.Systems
{
    /// <summary>
    ///     Generates and applies terrain textures to the mesh produced by earlier view subsystems.
    ///     Runs after <see cref="TerrainViewGenerationSubSystem" />.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewTextureSubSystem : ViewSubSystem
    {
        private const int ExecutionPriority = 200;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <inheritdoc />
        public override UniTask Update(GameState state, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
