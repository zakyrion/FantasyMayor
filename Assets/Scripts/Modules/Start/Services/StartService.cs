using Modules.Hexes.Components;
using Unity.Entities;
using UnityEngine;

public class StartService : MonoBehaviour
{
    private EntityArchetype _hexesShowUIArchetype;

    private void Start()
    {
        Debug.Log("[skh] StartService.Start()");
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _hexesShowUIArchetype = em.CreateArchetype(typeof(HexesShowUIEvent));

        em.CreateEntity(_hexesShowUIArchetype);
    }
}
