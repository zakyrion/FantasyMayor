using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        // Already decided at SymbolStart, straight off the symbol — a field's declared type never changes once
        // the symbol is known, so neither needs a sighting action over syntax.
        private readonly bool _loopContract;
        private readonly bool _holdsEventReader;

        private readonly ConcurrentBag<IEventSymbol> _subscribedEvents = new();

        public MarkedTypeCheck(INamedTypeSymbol type, ImmutableArray<AttributeData> claims, MarkerVocabulary vocabulary,
            bool loopContract, bool holdsEventReader)
        {
            _type = type;
            _claims = claims;
            _vocabulary = vocabulary;
            _loopContract = loopContract;
            _holdsEventReader = holdsEventReader;
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
                holdsEventReader: _holdsEventReader,
                subscribedEvents: _subscribedEvents.ToImmutableArray(),
                tagStruct: _type.TypeKind == TypeKind.Struct
                           && _type.AllInterfaces.Contains(_vocabulary.TagInterface, SymbolEqualityComparer.Default));

        private ImmutableArray<Diagnostic> FindMismatches(TypeShape shape)
        {
            var mismatches = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var claim in _claims)
            {
                // The type came from SymbolStart over source code, so every attribute on it has syntax.
                var location = claim.ApplicationSyntaxReference.GetSyntax().GetLocation();

                if (SymbolEqualityComparer.Default.Equals(claim.AttributeClass, _vocabulary.SystemRole))
                {
                    // A marker on a class with no EventReader field is forbidden outright, whether or not the
                    // class runs the Update loop; a class that DOES hold a reader still needs the loop contract
                    // for the marker's only legal value, PerFrame, to mean anything.
                    if (!shape.HoldsEventReader)
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.RoleMarkerForbidden, location, _type.Name));
                    else if (!shape.LoopContract)
                        mismatches.Add(Diagnostic.Create(MarkerShapeAnalyzer.PerFrameRoleMismatch, location, _type.Name));
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
    }
}
