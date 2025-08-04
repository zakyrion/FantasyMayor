using System.Threading;
using Modules.Hexes.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.Hexes.View.UI
{
    public class HexesUI : MonoBehaviour
    {
        [SerializeField]
        private Button _generateButton;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _clicked;
        private EntityArchetype _eventArchetype;

        private void Awake()
        {
            _generateButton.onClick.AddListener(GenerateHexes);

        }

        private void Start()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _eventArchetype = entityManager.CreateArchetype(ComponentType.ReadOnly<GenerateHexButtonEvent>());
        }

        private void OnDestroy()
        {
            _cancellationTokenSource.Cancel();
        }

        private void GenerateHexes()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.CreateEntity(_eventArchetype);
        }
    }
}
