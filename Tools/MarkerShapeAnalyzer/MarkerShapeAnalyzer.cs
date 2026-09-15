using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FantasyMayor.Analyzers
{
    // Fails the Unity compilation on every FantasyMayor marker — SystemRole, ViewSubscriber, TagLabel — whose type
    // does not have the shape the marker claims. A matching marker is silent; a redundant marker is not checked here.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MarkerShapeAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "FantasyMayor.Markers";

        internal static readonly DiagnosticDescriptor PerFrameRoleMismatch = new(
            "FM1001",
            "SystemRole(PerFrame) contradicts the class shape",
            "{0} claims SystemRole(PerFrame), but a per-frame system is a non-abstract IUpdatedSystem or ILateUpdatedSystem whose base(...) is not anchored on an event archetype",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReactiveRoleMismatch = new(
            "FM1002",
            "SystemRole(Reactive) contradicts the class shape",
            "{0} claims SystemRole(Reactive), but a reactive system is a non-abstract IUpdatedSystem or ILateUpdatedSystem, not anchored on a table in base(...), that anchors on or holds an event archetype",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ViewSubscriberMismatch = new(
            "FM1003",
            "ViewSubscriber contradicts the class shape",
            "{0} claims ViewSubscriber({1}), but {1} must derive from MonoBehaviour and {0} must add a handler with += to an event declared by {1} or its base",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor TagLabelMismatch = new(
            "FM1004",
            "TagLabel contradicts the type shape",
            "{0} claims TagLabel, but only a struct implementing ITag can be a label tag",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(PerFrameRoleMismatch, ReactiveRoleMismatch, ViewSubscriberMismatch, TagLabelMismatch);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(StartCompilation);
        }

        private static void StartCompilation(CompilationStartAnalysisContext context)
        {
            var vocabulary = MarkerVocabulary.Resolve(context.Compilation);
            context.RegisterSymbolStartAction(typeStart => StartType(typeStart, vocabulary), SymbolKind.NamedType);
        }

        private static void StartType(SymbolStartAnalysisContext context, MarkerVocabulary vocabulary)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            var claims = CollectClaims(type, vocabulary);

            // A type without a marker — nearly every type — registers nothing.
            if (claims.IsEmpty)
                return;

            var check = new MarkedTypeCheck(type, claims, vocabulary);
            context.RegisterSyntaxNodeAction(check.SightBaseAnchor, SyntaxKind.BaseConstructorInitializer);
            context.RegisterSyntaxNodeAction(check.SightHeldEventArchetype, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(check.SightSubscription, SyntaxKind.AddAssignmentExpression);
            context.RegisterSymbolEndAction(check.ReportMismatches);
        }

        private static ImmutableArray<AttributeData> CollectClaims(INamedTypeSymbol type, MarkerVocabulary vocabulary) =>
            type.GetAttributes()
                .Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.SystemRole)
                                    || SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.ViewSubscriber)
                                    || SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.TagLabel))
                .ToImmutableArray();
    }
}
