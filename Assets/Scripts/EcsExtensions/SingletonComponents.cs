using System;
using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Provides type-keyed access to singleton components while hiding their ECS backing store and row.
    /// </summary>
    public sealed class SingletonComponents
    {
        private readonly EntityStore _store;
        private readonly Entity _row;
        private readonly ComponentTypes _declaredComponents;
        private ComponentTypes _initializedComponents;

        internal SingletonComponents(in SingletonArchetypeDefinition archetype)
        {
            _declaredComponents = archetype.ComponentTypes;
            _store = new EntityStore();
            _row = _store.GetArchetype(archetype.ComponentTypes, archetype.Tags).CreateEntity();
        }

        public T Get<T>() where T : struct, IComponent
        {
            if (!_initializedComponents.Has<T>())
                throw new InvalidOperationException(
                    $"Singleton component '{typeof(T).Name}' has not been initialized.");

            return _row.GetComponent<T>();
        }

        public bool Has<T>() where T : struct, IComponent =>
            _initializedComponents.Has<T>();

        public void Set<T>(in T component) where T : struct, IComponent
        {
            if (!_declaredComponents.Has<T>())
                throw new InvalidOperationException(
                    $"Singleton component '{typeof(T).Name}' is not declared in SingletonArchetypes.Singleton.");

            _row.AddComponent(component);
            _initializedComponents.Add<T>();
        }
    }
}
