using System;
using System.Collections.Generic;

namespace DataLayer.Core
{
    public abstract class EntityDataManager<TKey> where TKey : class
    {
        protected Dictionary<Type, IContainer<TKey>> Containers = new();

        public void AddContainer(IContainer<TKey> container)
        {
            var type = typeof(IContainer<TKey>);
            Containers.Add(type, container);
        }

        public TContainer GetContainer<TContainer>() where TContainer : IContainer
        {
            var type = typeof(TContainer);
            return (TContainer)Containers[type];
        }

        public void RemoveContainer(IContainer<TKey> container)
        {
            var type = typeof(IContainer<TKey>);
            Containers.Remove(type);
        }
    }
}
