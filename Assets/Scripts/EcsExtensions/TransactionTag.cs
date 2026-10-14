using Friflo.Engine.ECS;

namespace EcsExtensions
{
    // Label of role transaction: the archetype that carries it is a transaction entity.
    [TagLabel(TagLabelRole.Transaction)]
    public struct TransactionTag : ITag
    {
    }
}
