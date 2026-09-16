using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FantasyMayor.Analyzers
{
    // One check per type that either carries a marker or could be the shape that demands one, alive from
    // SymbolStart to SymbolEnd: the sighting actions arrive for every node of every partial declaration,
    // possibly in parallel, and the report compares at the end.
    internal sealed class MarkedTypeCheck
    {
        private readonly INamedTypeSymbol _type;
        private readonly ImmutableArray<AttributeData> _claims;
        private readonly MarkerVocabulary _vocabulary;

        // Already decided at SymbolStart — it is what chose which sighting actions this check registers,
        // so it arrives here rather than being measured a second time.
        private readonly bool _loopContract;

        private readonly ConcurrentBag<BaseAnchor> _baseAnchors = new();
        private readonly ConcurrentBag<IMethodSymbol> _heldEventArchetypes = new();
        private readonly ConcurrentBag<IEventSymbol> _subscribedEvents = new();

        public MarkedTypeCheck(INamedTypeSymbol type, ImmutableArray<AttributeData> claims, MarkerVocabulary vocabulary,
            bool loopContract)
        {
            _type = type;
            _claims = claims;
            _vocabulary = vocabulary;
            _loopContract = loopContract;
        }

        public void SightBaseAnchor(SyntaxNodeAnalysisContext context)
        {
            // A nested type's constructor arrives here too; and base(...) of any other base is not a system's anchor.
            if (!SymbolEqualityComparer.Default.Equals(context.ContainingSymbol?.ContainingType, _type))
                return;
            if (!SymbolEqualityComparer.Default.Equals(_type.BaseType?.OriginalDefinition, _vocabulary.UpdatedSystemBase)
                && !SymbolEqualityComparer.Default.Equals(_type.BaseType?.OriginalDefinition, _vocabulary.LateUpdatedSystemBase))
                return;

            var anchor = BaseAnchor.Table;
            foreach (var argument in ((ConstructorInitializerSyntax)context.Node).ArgumentList.Arguments)
            {
                if (IsEventArchetypeCall(argument.Expression, context.SemanticModel))
                    anchor = BaseAnchor.Event;

                // AnyComponents(ComponentTypes.Get<…>()) whose every type argument is an event by the naming law.
                if (context.SemanticModel.GetSymbolInfo(argument.Expression).Symbol is IMethodSymbol { Name: "AnyComponents" }
                    && argument.Expression is InvocationExpressionSyntax { ArgumentList: { Arguments: { Count: 1 } anyArguments } }
                    && context.SemanticModel.GetSymbolInfo(anyArguments[0].Expression).Symbol is IMethodSymbol { Name: "Get" } get
                    && SymbolEqualityComparer.Default.Equals(get.ContainingType, _vocabulary.ComponentTypes)
                    && get.TypeArguments.All(component => component.TypeKind == TypeKind.Struct
                                                          && (component.Name.EndsWith("Event") || component.Name.EndsWith("EventComponent"))))
                    anchor = BaseAnchor.Event;
            }

            _baseAnchors.Add(anchor);
        }

        private bool IsEventArchetypeCall(ExpressionSyntax expression, SemanticModel semanticModel) =>
            semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol { Name: "Of" } method
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, _vocabulary.EventArchetypes);

        public void SightHeldEventArchetype(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // A nested type's call belongs to that type; a call inside base(...) is the anchor SightBaseAnchor records,
            // or no anchor at all under another base. A call inside this(...) is held like any other.
            if (!SymbolEqualityComparer.Default.Equals(context.ContainingSymbol?.ContainingType, _type)
                || invocation.FirstAncestorOrSelf<ConstructorInitializerSyntax>()?.IsKind(SyntaxKind.BaseConstructorInitializer) == true)
                return;

            if (IsEventArchetypeCall(invocation, context.SemanticModel))
                _heldEventArchetypes.Add((IMethodSymbol)context.SemanticModel.GetSymbolInfo(invocation).Symbol);
        }

        public void SightSubscription(SyntaxNodeAnalysisContext context)
        {
            // A nested type's += belongs to that type.
            if (!SymbolEqualityComparer.Default.Equals(context.ContainingSymbol?.ContainingType, _type))
                return;

            // += over a number or a string is not a subscription — only an event symbol on the left is.
            if (context.SemanticModel.GetSymbolInfo(((AssignmentExpressionSyntax)context.Node).Left).Symbol is IEventSymbol subscribedEvent)
                _subscribedEvents.Add(subscribedEvent);
        }

        public void ReportMismatches(SymbolAnalysisContext context)
        {
            var shape = MeasureShape();
            var mismatches = FindMismatches(shape);
            foreach (var mismatch in mismatches)
                context.ReportDiagnostic(mismatch);
        }

        private TypeShape MeasureShape() =>
            new(
                loopContract: _loopContract,
                eventAnchored: _baseAnchors.Contains(BaseAnchor.Event),
                tableAnchored: _baseAnchors.Contains(BaseAnchor.Table),
                heldEventArchetypes: _heldEventArchetypes.ToImmutableArray(),
                subscribedEvents: _subscribedEvents.ToImmutableArray(),
                tagStruct: _type.TypeKind == TypeKind.Struct
                           && _type.AllInterfaces.Contains(_vocabulary.TagInterface, SymbolEqualityComparer.Default),
                cleanup: MarkerShapeFacts.DerivesFrom(_type, _vocabulary.EventCleanupSystem));

        // The role order decides every shape but one, and the two branches that run ahead of the marker's branch
        // are cleanup and an event anchor in base(...). What is left — an Update-loop class that holds an event
        // archetype outside base(...) — is where the marker is required, and it is the only place it is allowed.
        private static bool MarkerDecidesRole(TypeShape shape) =>
            shape.LoopContract && !shape.Cleanup && !shape.EventAnchored && !shape.HeldEventArchetypes.IsEmpty;

        private static string DecidedRole(TypeShape shape)
        {
            if (shape.Cleanup)
                return "cleanup — it extends the global event cleanup system";
            if (shape.EventAnchored)
                return "reactive — its base(...) anchors on an event archetype";
            if (shape.LoopContract)
                return "per-frame — it runs the Update loop and holds no event archetype";

            return "no role at all — it does not run the Update loop";
        }

        private ImmutableArray<Diagnostic> FindMismatches(TypeShape shape)
        {
            var mismatches = ImmutableArray.CreateBuilder<Diagnostic>();

            if (MarkerDecidesRole(shape) && !CarriesRoleMarker())
                mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.RoleMarkerMissing, _type.Locations[0], _type.Name));

            foreach (var claim in _claims)
            {
                // The type came from SymbolStart over source code, so every attribute on it has syntax.
                var location = claim.ApplicationSyntaxReference.GetSyntax().GetLocation();

                if (SymbolEqualityComparer.Default.Equals(claim.AttributeClass, _vocabulary.SystemRole))
                {
                    var role = (SystemRoleKind)(int)claim.ConstructorArguments[0].Value;
                    var perFrameShape = shape.LoopContract && !shape.EventAnchored;
                    var reactiveShape = shape.LoopContract && !shape.TableAnchored
                                                           && (shape.EventAnchored || !shape.HeldEventArchetypes.IsEmpty);
                    var valueFits = role == SystemRoleKind.PerFrame ? perFrameShape : reactiveShape;

                    if (role == SystemRoleKind.PerFrame && !perFrameShape)
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.PerFrameRoleMismatch, location, _type.Name));
                    if (role == SystemRoleKind.Reactive && !reactiveShape)
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.ReactiveRoleMismatch, location, _type.Name));

                    // A value that already contradicts the shape is the sharper finding; redundancy is reported
                    // only where the value itself was right and the marker still had nothing to decide.
                    if (valueFits && !MarkerDecidesRole(shape))
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.RoleMarkerRedundant, location, _type.Name, DecidedRole(shape)));
                }
                else if (SymbolEqualityComparer.Default.Equals(claim.AttributeClass, _vocabulary.ViewSubscriber))
                {
                    var view = (INamedTypeSymbol)claim.ConstructorArguments[0].Value;

                    // The event may be declared by a base of the view, so the view derives from its declaring type.
                    var subscribesToView = shape.SubscribedEvents.Any(subscribedEvent =>
                        MarkerShapeFacts.DerivesFrom(view, subscribedEvent.ContainingType));

                    if (!MarkerShapeFacts.DerivesFrom(view, _vocabulary.MonoBehaviour) || !subscribesToView)
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.ViewSubscriberMismatch, location, _type.Name, view.Name));
                }
                else if (!shape.TagStruct)
                {
                    // The remaining claim is TagLabel.
                    mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.TagLabelMismatch, location, _type.Name));
                }
            }

            return mismatches.ToImmutable();
        }

        private bool CarriesRoleMarker() =>
            _claims.Any(claim => SymbolEqualityComparer.Default.Equals(claim.AttributeClass, _vocabulary.SystemRole));
    }

    // What one base(...) of the type's constructor stands on.
    internal enum BaseAnchor
    {
        Event,
        Table
    }
}
