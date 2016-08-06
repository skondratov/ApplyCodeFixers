using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleCop.Analyzers.Helpers;

namespace AbbreviationFix
{
    /// <summary>
    /// Abbreviations are not allowed except officially register. Apply common naming rules for them
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AbbreviationFixCodeFixProvider))]
    [Shared]
    public class AbbreviationFixCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AbbreviationFixAnalyzer.DiagnosticId); }
        }

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var abbreveatures = RenameHelper.GetAbbreveaturesInSymbol(token);
                var originalName = token.ValueText;
                var memberSyntax = RenameHelper.GetParentDeclaration(token);

                var prefix = string.Empty;
                var name = new StringBuilder();
                foreach (Match abbreveature in abbreveatures)
                {
                    name.Append(originalName.Substring(name.Length, abbreveature.Index - name.Length));

                    StringBuilder subRet = new StringBuilder(abbreveature.Value.ToLower());
                    subRet[0] = char.ToUpper(subRet[0]);

                    // Handle variable names which starts with '_'
                    if (abbreveature.Index == 1 && originalName.StartsWith("_"))
                    {
                        subRet[0] = char.ToLower(subRet[0]);
                    }

                    if (abbreveature.Index == 0)
                    {
                        if (memberSyntax is ParameterSyntax)
                        {
                            subRet[0] = char.ToLower(subRet[0]);
                        }

                        if (memberSyntax is VariableDeclaratorSyntax)
                        {
                            if (memberSyntax.Parent.Parent is FieldDeclarationSyntax)
                            {
                                var accessibility = (memberSyntax.Parent.Parent as FieldDeclarationSyntax).GetDeclaredAccessibility(
                                    await context.Document.GetSemanticModelAsync(context.CancellationToken),
                                    context.CancellationToken);

                                if (accessibility <= Accessibility.Private)
                                {
                                    prefix = "_";
                                    subRet[0] = char.ToLower(subRet[0]);
                                }
                            }

                            subRet[0] = char.ToLower(subRet[0]);
                        }

                        if (memberSyntax is InterfaceDeclarationSyntax && abbreveature.Value[0] == 'I')
                        {
                            subRet[1] = char.ToUpper(subRet[1]);
                        }
                    }

                    if (originalName.Length != subRet.Length + name.Length)
                    {
                        subRet[subRet.Length - 1] = char.ToUpper(subRet[subRet.Length - 1]);
                    }

                    name.Append(subRet);
                }

                name.Append(originalName.Substring(name.Length, originalName.Length - name.Length));

                var newName = prefix + name;

                if (memberSyntax is NamespaceDeclarationSyntax)
                {
                    // namespaces are not symbols. So we are just renaming the namespace
                    Func<CancellationToken, Task<Document>> renameNamespace = cancellationToken =>
                    {
                        IdentifierNameSyntax identifierSyntax = (IdentifierNameSyntax)token.Parent;

                        var newIdentifierSyntax = identifierSyntax.WithIdentifier(SyntaxFactory.Identifier(newName));

                        var newRoot = root.ReplaceNode(identifierSyntax, newIdentifierSyntax);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    };

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format("Rename to {0}", newName),
                            renameNamespace,
                            nameof(AbbreviationFixCodeFixProvider) + "_" + diagnostic.Id),
                        diagnostic);
                }
                else if (memberSyntax != null)
                {
                    SemanticModel semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                    var declaredSymbol = semanticModel.GetDeclaredSymbol(memberSyntax);
                    if (declaredSymbol == null)
                    {
                        continue;
                    }

                    int index = 0;
                    var baseName = newName;
                    while (!await RenameHelper.IsValidNewMemberNameAsync(semanticModel, declaredSymbol, newName, context.CancellationToken).ConfigureAwait(false))
                    {
                        index++;
                        newName = baseName + index;
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format("Rename to {0}", newName),
                            cancellationToken => RenameHelper.RenameSymbolAsync(document, root, token, newName, cancellationToken),
                            nameof(AbbreviationFixCodeFixProvider) + "_" + diagnostic.Id),
                        diagnostic);
                }
            }
        }
    }
}
