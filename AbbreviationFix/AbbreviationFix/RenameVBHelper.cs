using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace AbbreviationFix
{
    class RenameVBHelper
    {
        public static SyntaxNode GetParentDeclaration(SyntaxToken token)
        {
            SyntaxNode parent = token.Parent;

            while (parent != null)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.Parameter:
                    case SyntaxKind.VariableDeclarator:
                        return parent;
                    default:
                        var declarationParent = parent as DeclarationStatementSyntax;
                        if (declarationParent != null)
                        {
                            return declarationParent;
                        }

                        break;
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
