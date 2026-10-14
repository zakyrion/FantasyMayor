using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;

namespace Domains.Map.Generation.Systems
{
    internal abstract class GenerationSubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        public Type OrchestratorType => typeof(GenerationSystem);

        public abstract UniTask Update(CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}
