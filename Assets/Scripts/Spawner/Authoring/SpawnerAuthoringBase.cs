using System.Collections.Generic;
using UnityEngine;

namespace Spawner.Authoring
{
    public abstract class SpawnerAuthoringBase : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _prefabs;

        public List<GameObject> Prefabs => _prefabs;
    }
}
