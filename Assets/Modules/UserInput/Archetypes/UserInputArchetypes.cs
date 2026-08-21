using Friflo.Engine.ECS;
using Modules.UserInput.Components;
using Modules.UserInput.Tags;
// Modules.UserInput.Tags (our namespace) shadows Friflo's Tags type inside Modules.UserInput.* — alias it.
using EcsTags = Friflo.Engine.ECS.Tags;

namespace Modules.UserInput.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the UserInput assembly. Each method returns the live
    ///     <see cref="Archetype" /> — entities are created BY it and iterated THROUGH it; the caller keeps
    ///     it in its own field. This holder stores nothing.
    ///     The row itself is created by Installers.World at app start — the archetype is resolved here
    ///     because this assembly owns both the component and the tag.
    /// </summary>
    public static class UserInputArchetypes
    {
        public static Archetype PlayerInput(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<PlayerInputComponent>(),
                EcsTags.Get<PlayerInputTag>());
    }
}
