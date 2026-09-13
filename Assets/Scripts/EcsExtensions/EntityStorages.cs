using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using UnityEngine;

namespace EcsExtensions
{
    // Configs are ScriptableObjects loaded once at startup and immutable for the whole session:
    // the storage only keeps the references, it never releases them.
    public sealed class EntityStorages
    {
        private readonly Dictionary<Type, ScriptableObject> _configs = new();

        public EntityStorages(in SingletonArchetypeDefinition singletonArchetype)
        {
            World = new EntityStore();
            Singletons = new SingletonComponents(singletonArchetype);
        }

        public EntityStore World { get; }
        public SingletonComponents Singletons { get; }

        public void Add<T>(T config) where T : ScriptableObject
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), $"Config {typeof(T).Name} is null.");

            _configs[typeof(T)] = config;
        }

        public T Get<T>() where T : ScriptableObject
        {
            if (!_configs.TryGetValue(typeof(T), out var config))
                throw new InvalidOperationException($"Config {typeof(T).Name} is not loaded.");

            return (T)config;
        }
    }
}
