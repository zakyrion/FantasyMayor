using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FantasyMayor.Analyzers
{
    // The facts every check reads off a symbol, with no state of their own: ancestry, the view folder, and the
    // markers a type carries.
    internal static class MarkerShapeFacts
    {
        private const string ViewsFolder = "Views";

        public static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol ancestor)
        {
            if (ancestor == null)
                return false;

            for (var candidate = type; candidate != null; candidate = candidate.BaseType)
                if (SymbolEqualityComparer.Default.Equals(candidate, ancestor))
                    return true;

            return false;
        }

        // The view layer is EVERYTHING declared under a Views/ folder — the file path decides it, never the
        // ancestry. A type that reaches this compilation as metadata has no path, so no assembly but the one
        // that declares a view can be asked about it.
        public static bool DeclaredUnderViewsFolder(INamedTypeSymbol type)
        {
            foreach (var declaration in type.DeclaringSyntaxReferences)
                if (HasViewsSegment(declaration.SyntaxTree.FilePath))
                    return true;

            return false;
        }

        // A whole path segment named Views, under either separator — "Reviews/" and "ViewsModel/" are not it.
        private static bool HasViewsSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            for (var start = path.IndexOf(ViewsFolder, StringComparison.Ordinal);
                 start >= 0;
                 start = path.IndexOf(ViewsFolder, start + 1, StringComparison.Ordinal))
            {
                var end = start + ViewsFolder.Length;
                var opensSegment = start == 0 || IsSeparator(path[start - 1]);
                var closesSegment = end < path.Length && IsSeparator(path[end]);

                if (opensSegment && closesSegment)
                    return true;
            }

            return false;
        }

        private static bool IsSeparator(char character) => character == '/' || character == '\\';

        public static ImmutableArray<AttributeData> CollectClaims(INamedTypeSymbol type, MarkerVocabulary vocabulary) =>
            type.GetAttributes()
                .Where(attribute => IsMarker(attribute, vocabulary))
                .ToImmutableArray();

        public static bool IsMarker(AttributeData attribute, MarkerVocabulary vocabulary) =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.SystemRole)
            || SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.ViewSubscriber)
            || SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.TagLabel);

        // The marker names the view whose base declares the event just as well as the view itself.
        public static bool NamesTheView(INamedTypeSymbol subscriber, INamedTypeSymbol declaringView, MarkerVocabulary vocabulary)
        {
            foreach (var attribute in subscriber.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.ViewSubscriber))
                    continue;
                if (attribute.ConstructorArguments.Length != 1)
                    continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol named && DerivesFrom(named, declaringView))
                    return true;
            }

            return false;
        }
    }
}
