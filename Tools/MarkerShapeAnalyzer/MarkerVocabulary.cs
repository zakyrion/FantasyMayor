using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The three marker types and the shape anchors as one compilation sees them. A type the compilation does
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

        // The one global cleanup system. The role order gives it the first branch, ahead of the branch the
        // marker decides, so a class that is or extends it never needs a marker and never may carry one.
        public readonly INamedTypeSymbol EventCleanupSystem;

        // How the marker declarations themselves forbid inheritance.
        public readonly INamedTypeSymbol AttributeUsage;

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
            EventCleanupSystem = compilation.GetTypeByMetadataName("EcsExtensions.EventCleanupSystem");
            AttributeUsage = compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
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
