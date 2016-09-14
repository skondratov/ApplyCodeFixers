using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using StyleCop.Analyzers;
using StyleCop.Analyzers.Helpers;

namespace AbbreviationFix
{
    /// <summary>
    /// Abbreviations are not allowed except officially register. Apply common naming rules for them
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(AbbreviationVBFixProvider))]
    [Shared]
    public class AbbreviationVBFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AbbreviationVBAnalyzer.DiagnosticId); }
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
            var settings = document.Project.AnalyzerOptions.GetStyleCopSettings(context.CancellationToken);
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var abbreviations = AbbreviationHelper.GetAbbreviationsInSymbol(token, settings.AbbreviationRules);
                var originalName = token.ValueText;
                var memberSyntax = RenameVBHelper.GetParentDeclaration(token);

                var prefix = string.Empty;
                var name = new StringBuilder();
                foreach (Match abbreviation in abbreviations)
                {
                    name.Append(originalName.Substring(name.Length, abbreviation.Index - name.Length));

                    StringBuilder subRet = new StringBuilder(abbreviation.Value.ToLower());
                    subRet[0] = char.ToUpper(subRet[0]);

                    // Handle variable names which starts with '_'
                    if (abbreviation.Index == 1 && originalName.StartsWith("_"))
                    {
                        subRet[0] = char.ToLower(subRet[0]);
                    }

                    if (abbreviation.Index == 0)
                    {
                        if (memberSyntax is ParameterSyntax)
                        {
                            subRet[0] = char.ToLower(subRet[0]);
                        }

                        if (memberSyntax is VariableDeclaratorSyntax)
                        {
                            if (memberSyntax.Parent is FieldDeclarationSyntax)
                            {
                                var accessibility = ((FieldDeclarationSyntax)memberSyntax.Parent)
                                    .GetDeclaredAccessibility(
                                        await context.Document.GetSemanticModelAsync(context.CancellationToken),
                                        context.CancellationToken);

                                if (accessibility <= Accessibility.Private)
                                {
                                    prefix = "_";
                                    subRet[0] = char.ToLower(subRet[0]);
                                }
                            }
                            else
                            {
                                subRet[0] = char.ToLower(subRet[0]);
                            }
                        }
                    }

                    // According to ReSharper next sumbol afte digit - must be in uppercase
                    var firstAfterDigit = Regex.Match(abbreviation.Value, @"^\d+([A-Z])[^\d]");
                    if (firstAfterDigit.Success &&
                        firstAfterDigit.Groups.Count > 1)
                    {
                        var capture = firstAfterDigit.Groups[1];
                        var charAfterDigit = capture.Value.ToUpper()[0];
                        subRet[capture.Index] = charAfterDigit;
                    }

                    // If last symbol in match no last in word - do capitalize, due to PascalCase
                    if (originalName.Length != subRet.Length + name.Length)
                    {
                        subRet[subRet.Length - 1] = char.ToUpper(subRet[subRet.Length - 1]);
                    }

                    name.Append(subRet);
                }

                name.Append(originalName.Substring(name.Length, originalName.Length - name.Length));

                var newName = prefix + name;

                if (memberSyntax != null)
                {
                    /*
                     * VB.NET - don't check existing members, because we change case only.
                     */

                    Debug.WriteLine("{0}|{1}|{2}", token.ValueText, newName, document.Name);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format("Rename to {0}", newName),
                            cancellationToken => RenameHelper.RenameSymbolAsync(document, root, token, newName, cancellationToken),
                            nameof(AbbreviationVBFixProvider) + "_" + diagnostic.Id),
                        diagnostic);
                }
            }
        }
    }
}
