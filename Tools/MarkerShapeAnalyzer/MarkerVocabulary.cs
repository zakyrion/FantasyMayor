using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The three marker types and the eight shape anchors as one compilation sees them. A type the compilation does
    // not see stays null — that shape cannot occur there.
    internal sealed class MarkerVocabulary
    {
        public readonly INamedTypeSymbol SystemRole;
        public readonly INamedTypeSymbol ViewSubscriber;
        public readonly INamedTypeSymbol TagLabel;

        public readonly INamedTypeSymbol UpdatedSystemInterface;
        public readonly INamedTypeSymbol LateUpdatedSystemInterface;
        public readonly INamedTypeSymbol UpdatedSystemBase;
        public readonly INamedTypeSymbol LateUpdatedSystemBase;
        public readonly INamedTypeSymbol EventArchetypes;
        public readonly INamedTypeSymbol ComponentTypes;
        public readonly INamedTypeSymbol TagInterface;
        public readonly INamedTypeSymbol MonoBehaviour;

        private MarkerVocabulary(Compilation compilation)
        {
            SystemRole = compilation.GetTypeByMetadataName("EcsExtensions.SystemRoleAttribute");
            ViewSubscriber = compilation.GetTypeByMetadataName("EcsExtensions.ViewSubscriberAttribute");
            TagLabel = compilation.GetTypeByMetadataName("EcsExtensions.TagLabelAttribute");

            UpdatedSystemInterface = compilation.GetTypeByMetadataName("EcsExtensions.IUpdatedSystem");
            LateUpdatedSystemInterface = compilation.GetTypeByMetadataName("EcsExtensions.ILateUpdatedSystem");
            UpdatedSystemBase = compilation.GetTypeByMetadataName("EcsExtensions.UpdatedSystem");
            LateUpdatedSystemBase = compilation.GetTypeByMetadataName("EcsExtensions.LateUpdatedSystem");
            EventArchetypes = compilation.GetTypeByMetadataName("EcsExtensions.EventArchetypes");
            ComponentTypes = compilation.GetTypeByMetadataName("Friflo.Engine.ECS.ComponentTypes");
            TagInterface = compilation.GetTypeByMetadataName("Friflo.Engine.ECS.ITag");
            MonoBehaviour = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
        }

        public static MarkerVocabulary Resolve(Compilation compilation) => new(compilation);
    }

    // The analyzer's copy of EcsExtensions.SystemRoleKind — the attribute argument arrives as its underlying int.
    internal enum SystemRoleKind
    {
        PerFrame,
        Reactive
    }
}
