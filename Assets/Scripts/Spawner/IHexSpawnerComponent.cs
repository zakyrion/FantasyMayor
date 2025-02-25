using Unity.Entities;

namespace HW.Authoring
{
    public interface IHexSpawnerComponent : IBufferElementData
    {
        Entity Prefab { get; set; }
    }
}