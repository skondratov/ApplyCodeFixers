namespace AbbreviationFix
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;
    using StyleCop.Analyzers.Helpers;

    public static class AccessLevelVBHelper
    {
        internal static Accessibility GetDeclaredAccessibility(this FieldDeclarationSyntax syntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Requires.NotNull(syntax, nameof(syntax));
            Requires.NotNull(semanticModel, nameof(semanticModel));

            AccessLevel accessLevel = AccessLevelHelper.GetAccessLevel(syntax.Modifiers);
            if (accessLevel != AccessLevel.NotSpecified)
            {
                return accessLevel.ToAccessibility();
            }

            if (syntax.IsKind(SyntaxKind.FieldDeclaration))
            {
                return Accessibility.Private;
            }

            VariableDeclaratorSyntax firstVariable = syntax.Declarators.FirstOrDefault();
            if (firstVariable == null)
            {
                return Accessibility.NotApplicable;
            }

            ISymbol declaredSymbol = semanticModel.GetDeclaredSymbol(firstVariable, cancellationToken);
            return declaredSymbol?.DeclaredAccessibility ?? Accessibility.NotApplicable;
        }
    }
}
