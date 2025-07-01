using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace DataLayer.Core
{
    [UsedImplicitly]
    public class DataLayer
    {
        private readonly List<IDataContainer> _dataContainers;

        [Inject]
        public DataLayer(List<IDataContainer> dataContainers)
        {
            Debug.Log($"[skh] DataLayer constructor. Containers count: {dataContainers.Count}");
            _dataContainers = dataContainers;
        }

        public T GetData<T>() where T : IDataContainer
        {
            return (T)_dataContainers.Find(x => x is T);
        }
    }
}
