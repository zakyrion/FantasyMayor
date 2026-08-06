using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Owns the single hidden row for world-scoped runtime state and configuration components.
    /// </summary>
    public sealed class SingletonStorage
    {
        private readonly EntityStore _store;
        private readonly Entity _row;

        public SingletonStorage()
        {
            _store = new EntityStore();
            _row = _store.CreateEntity();
        }

        public T Get<T>() where T : struct, IComponent =>
            _row.GetComponent<T>();

        public bool Has<T>() where T : struct, IComponent =>
            _row.HasComponent<T>();

        public void Set<T>(in T component) where T : struct, IComponent =>
            _row.AddComponent(component);
    }
}
