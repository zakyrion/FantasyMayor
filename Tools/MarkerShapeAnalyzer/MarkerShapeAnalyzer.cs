using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FantasyMayor.Analyzers
{
    // Fails the Unity compilation on every FantasyMayor marker law: a marker whose type does not have the shape
    // it claims, a marker missing from the one shape that needs one, a marker on a shape that decides itself, a
    // view subscription that names no view, and a marker declaration that lets itself be inherited. Every
    // message carries the id of the rule it enforces, in the form "rule <prefix>/<slug>".
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MarkerShapeAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "FantasyMayor.Markers";

        internal static readonly DiagnosticDescriptor PerFrameRoleMismatch = new(
            "FM1001",
            "SystemRole(PerFrame) contradicts the class shape",
            "{0} claims SystemRole(PerFrame), but a per-frame system is a non-abstract IUpdatedSystem or ILateUpdatedSystem whose base(...) is not anchored on an event archetype — rule system/marker-value",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReactiveRoleMismatch = new(
            "FM1002",
            "SystemRole(Reactive) contradicts the class shape",
            "{0} claims SystemRole(Reactive), but a reactive system is a non-abstract IUpdatedSystem or ILateUpdatedSystem, not anchored on a table in base(...), that anchors on or holds an event archetype — rule system/marker-value",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ViewSubscriberMismatch = new(
            "FM1003",
            "ViewSubscriber contradicts the class shape",
            "{0} claims ViewSubscriber({1}), but {1} must derive from MonoBehaviour and {0} must add a handler with += to an event declared by {1} or its base — rule view/subscriber-marker",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor TagLabelMismatch = new(
            "FM1004",
            "TagLabel contradicts the type shape",
            "{0} claims TagLabel, but only a struct implementing ITag can be a label tag — rule tag/label-marker",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor RoleMarkerMissing = new(
            "FM1005",
            "The role marker is missing on the shape no ordering rule decides",
            "{0} is an Update-loop class that holds an event archetype outside base(...) — the one shape the role order leaves open — so it carries [SystemRole(SystemRoleKind.Reactive)] or [SystemRole(SystemRoleKind.PerFrame)] — rule system/marker-required",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor RoleMarkerRedundant = new(
            "FM1006",
            "The role marker is redundant — the class shape already decides its role",
            "{0} carries a SystemRole marker, but its own shape already decides its role ({1}); the marker belongs only on an Update-loop class that holds an event archetype outside base(...) — rule system/marker-forbidden",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ViewSubscriptionUnmarked = new(
            "FM1007",
            "A subscription to a view's event carries no ViewSubscriber marker",
            "{0} adds a handler with += to {2}, an event declared by the view {1}, so it carries [ViewSubscriber(typeof({1}))] — rule view/subscriber-marker",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MarkerDeclaredInheritable = new(
            "FM1008",
            "A marker is declared inheritable",
            "{0} is a FantasyMayor marker, so it is declared [AttributeUsage(..., Inherited = false)]: no marker is inherited, every concrete class or struct carries its own — rule system/marker-not-inherited",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(PerFrameRoleMismatch, ReactiveRoleMismatch, ViewSubscriberMismatch, TagLabelMismatch,
                RoleMarkerMissing, RoleMarkerRedundant, ViewSubscriptionUnmarked, MarkerDeclaredInheritable);

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

            // The one question no marked type can answer: which class subscribes to a view without saying so.
            // It is asked of the += node itself, so the compilation pays for it once and only on +=.
            if (vocabulary.ViewSubscriber != null)
                context.RegisterSyntaxNodeAction(subscription => SightUnmarkedSubscription(subscription, vocabulary),
                    SyntaxKind.AddAssignmentExpression);
        }

        private static void StartType(SymbolStartAnalysisContext context, MarkerVocabulary vocabulary)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            // A marker declaration is checked as a declaration, not as a marked type.
            if (IsMarkerDeclaration(type, vocabulary))
            {
                context.RegisterSymbolEndAction(end => ReportInheritableMarker(end, (INamedTypeSymbol)end.Symbol, vocabulary));
                return;
            }

            var claims = MarkerShapeFacts.CollectClaims(type, vocabulary);
            var loopContract = HasLoopContract(type, vocabulary);

            // A type that carries no marker and cannot be the shape that demands one — nearly every type —
            // registers nothing.
            if (claims.IsEmpty && !loopContract)
                return;

            var check = new MarkedTypeCheck(type, claims, vocabulary, loopContract);

            // base(...) is an anchor only under the two system bases; nothing else can produce one.
            if (SymbolEqualityComparer.Default.Equals(type.BaseType?.OriginalDefinition, vocabulary.UpdatedSystemBase)
                || SymbolEqualityComparer.Default.Equals(type.BaseType?.OriginalDefinition, vocabulary.LateUpdatedSystemBase))
                context.RegisterSyntaxNodeAction(check.SightBaseAnchor, SyntaxKind.BaseConstructorInitializer);

            // A held event archetype only ever decides the role of a class that runs in the Update loop.
            if (loopContract)
                context.RegisterSyntaxNodeAction(check.SightHeldEventArchetype, SyntaxKind.InvocationExpression);

            // Subscriptions are read only to answer a ViewSubscriber marker the type already carries.
            if (claims.Any(claim => SymbolEqualityComparer.Default.Equals(claim.AttributeClass, vocabulary.ViewSubscriber)))
                context.RegisterSyntaxNodeAction(check.SightSubscription, SyntaxKind.AddAssignmentExpression);

            context.RegisterSymbolEndAction(check.ReportMismatches);
        }

        private static bool HasLoopContract(INamedTypeSymbol type, MarkerVocabulary vocabulary) =>
            !type.IsAbstract
            && (type.AllInterfaces.Contains(vocabulary.UpdatedSystemInterface, SymbolEqualityComparer.Default)
                || type.AllInterfaces.Contains(vocabulary.LateUpdatedSystemInterface, SymbolEqualityComparer.Default));

        private static bool IsMarkerDeclaration(INamedTypeSymbol type, MarkerVocabulary vocabulary) =>
            SymbolEqualityComparer.Default.Equals(type, vocabulary.SystemRole)
            || SymbolEqualityComparer.Default.Equals(type, vocabulary.ViewSubscriber)
            || SymbolEqualityComparer.Default.Equals(type, vocabulary.TagLabel);

        // AttributeUsage defaults Inherited to true, so a missing usage and a missing argument both read as
        // inheritable — only the written false holds the law.
        private static void ReportInheritableMarker(SymbolAnalysisContext context, INamedTypeSymbol marker, MarkerVocabulary vocabulary)
        {
            var usage = marker.GetAttributes()
                .FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, vocabulary.AttributeUsage));

            var inherited = usage?.NamedArguments
                .FirstOrDefault(argument => argument.Key == "Inherited")
                .Value.Value;

            if (inherited is false)
                return;

            var location = usage?.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? marker.Locations[0];
            context.ReportDiagnostic(Diagnostic.Create(MarkerDeclaredInheritable, location, marker.Name));
        }

        private static void SightUnmarkedSubscription(SyntaxNodeAnalysisContext context, MarkerVocabulary vocabulary)
        {
            // += over a number or a string is not a subscription — only an event symbol on the left is.
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol is not IEventSymbol subscribedEvent)
                return;

            var view = subscribedEvent.ContainingType;
            if (view == null || !MarkerShapeFacts.DeclaredUnderViewsFolder(view))
                return;

            var subscriber = context.ContainingSymbol?.ContainingType;
            if (subscriber == null)
                return;

            // A view wiring its own event — or a heir of it — is the view talking to itself, not a subscriber
            // crossing the boundary the marker records.
            if (MarkerShapeFacts.DerivesFrom(subscriber, view))
                return;

            if (MarkerShapeFacts.NamesTheView(subscriber, view, vocabulary))
                return;

            context.ReportDiagnostic(Diagnostic.Create(ViewSubscriptionUnmarked, assignment.GetLocation(),
                subscriber.Name, view.Name, subscribedEvent.Name));
        }
    }
}
