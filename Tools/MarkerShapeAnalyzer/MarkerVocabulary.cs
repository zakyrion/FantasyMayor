using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The two marker types and the shape anchors as one compilation sees them. A type the compilation does
    // not see stays null — that shape cannot occur there.
    internal sealed class MarkerVocabulary
    {
        public readonly INamedTypeSymbol SystemRole;
        public readonly INamedTypeSymbol ViewSubscriber;
        public readonly INamedTypeSymbol TagLabel;

        public readonly INamedTypeSymbol UpdatedSystemInterface;
        public readonly INamedTypeSymbol LateUpdatedSystemInterface;
        public readonly INamedTypeSymbol EventReader;
        public readonly INamedTypeSymbol TagInterface;
        public readonly INamedTypeSymbol MonoBehaviour;

        // How the marker declarations themselves forbid inheritance.
        public readonly INamedTypeSymbol AttributeUsage;

        private MarkerVocabulary(Compilation compilation)
        {
            SystemRole = compilation.GetTypeByMetadataName("EcsExtensions.SystemRoleAttribute");
            ViewSubscriber = compilation.GetTypeByMetadataName("EcsExtensions.ViewSubscriberAttribute");
            TagLabel = compilation.GetTypeByMetadataName("EcsExtensions.TagLabelAttribute");

            UpdatedSystemInterface = compilation.GetTypeByMetadataName("EcsExtensions.IUpdatedSystem");
            LateUpdatedSystemInterface = compilation.GetTypeByMetadataName("EcsExtensions.ILateUpdatedSystem");
            EventReader = compilation.GetTypeByMetadataName("EcsExtensions.EventReader`1");
            TagInterface = compilation.GetTypeByMetadataName("Friflo.Engine.ECS.ITag");
            MonoBehaviour = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
            AttributeUsage = compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        }

        public static MarkerVocabulary Resolve(Compilation compilation) => new(compilation);
    }

    // The analyzer's own copy of EcsExtensions.SystemRoleKind — the attribute argument arrives as its underlying
    // int. [rule system/marker-vocabulary-parity]: this copy must match the enumeration in EcsExtensions exactly.
    internal enum SystemRoleKind
    {
        PerFrame
    }
}
