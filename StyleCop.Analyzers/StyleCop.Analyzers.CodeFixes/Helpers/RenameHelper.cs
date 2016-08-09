// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Rename;

    internal static class RenameHelper
    {
        /// <summary>
        /// Match opts (usefull for test):
        /// public static int NAME +
        /// public static int NameDDisable3DD +
        /// public static int Name3DDaDDaDD ++
        /// public static int Name3DS1+
        /// public static int NameDX3+
        /// public static int DX3name +
        /// public static int D3Xcase; -
        /// public static int Name773DB33TFTname222DXS +++
        /// public static int Name33nA -
        /// </summary>
        /// <param name="syntaxToken"></param>
        /// <returns></returns>
        internal static IEnumerable<Match> GetAbbreveaturesInSymbol(SyntaxToken syntaxToken)
        {
            var regex = @"\d+[A-Z]{2,}$|\d+[A-Z]{3,}|[A-Z]{2,}$|[A-Z]{2,}\d+|[A-Z]{3,}";

            Func<SyntaxToken, bool> isInterface =
                token => token.Parent is InterfaceDeclarationSyntax && syntaxToken.ValueText[0] == 'I';
            if (isInterface(syntaxToken))
            {
                // Ignore I symbol for interfaces. ReSharper abbreveature fixing logic does not consider 'I'. E.g. IDDeal is not abbreviation.
                regex = $@"^I|({regex})";
            }

            var matches = Regex.Matches(syntaxToken.ValueText, regex);

            foreach (Match match in matches)
            {
                if (isInterface(syntaxToken) &&
                    match == matches[0])
                {
                    // [UGLY]: Ignore first match. Will be happy if someone will rewrite regex to avoid these tricks in code
                    continue;
                }

                var length = match.Index + match.Value.Length;
                if (syntaxToken.ValueText.Length < length)
                {
                    length++;
                }

                // Check registred abbreviations. Handle them in ReSharper way. If word continues after abbreviation
                // last capital letter is not included to abbreviation - it is start of new word.
                var onlySymbols =
                    Regex.Match(
                        syntaxToken.ValueText.Substring(match.Index, length - match.Index),
                        @"([A-Z]{2,})(?![a-z])");

                if (DiagnosticConfig.AbbreviationsToSkip.Contains(onlySymbols.Value))
                {
                    continue;
                }

                yield return match;
            }
        }

        public static async Task<Solution> RenameSymbolAsync(Document document, SyntaxNode root, SyntaxToken declarationToken, string newName, CancellationToken cancellationToken)
        {
            var annotatedRoot = root.ReplaceToken(declarationToken, declarationToken.WithAdditionalAnnotations(RenameAnnotation.Create()));
            var annotatedSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, annotatedRoot);
            var annotatedDocument = annotatedSolution.GetDocument(document.Id);

            annotatedRoot = await annotatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var annotatedToken = annotatedRoot.FindToken(declarationToken.SpanStart);

            var semanticModel = await annotatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(annotatedToken.Parent, cancellationToken);

            var newSolution = await Renamer.RenameSymbolAsync(annotatedSolution, symbol, newName, null, cancellationToken).ConfigureAwait(false);

            // TODO: return annotatedSolution instead of newSolution if newSolution contains any new errors (for any project)
            return newSolution;
        }

        public static async Task<bool> IsValidNewMemberNameAsync(SemanticModel semanticModel, ISymbol symbol, string name, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.NamedType)
            {
                TypeKind typeKind = ((INamedTypeSymbol)symbol).TypeKind;

                // If the symbol is a class or struct, the name can't be the same as any of its members.
                if (typeKind == TypeKind.Class || typeKind == TypeKind.Struct)
                {
                    var members = (symbol as INamedTypeSymbol)?.GetMembers(name);
                    if (members.HasValue && !members.Value.IsDefaultOrEmpty)
                    {
                        return false;
                    }
                }
            }

            var containingSymbol = symbol.ContainingSymbol;

            var containingNamespaceOrTypeSymbol = containingSymbol as INamespaceOrTypeSymbol;
            if (containingNamespaceOrTypeSymbol != null)
            {
                if (containingNamespaceOrTypeSymbol.Kind == SymbolKind.Namespace)
                {
                    // Make sure to use the compilation namespace so interfaces in referenced assemblies are considered
                    containingNamespaceOrTypeSymbol = semanticModel.Compilation.GetCompilationNamespace((INamespaceSymbol)containingNamespaceOrTypeSymbol);
                }
                else if (containingNamespaceOrTypeSymbol.Kind == SymbolKind.NamedType)
                {
                    TypeKind typeKind = ((INamedTypeSymbol)containingNamespaceOrTypeSymbol).TypeKind;

                    // If the containing type is a class or struct, the name can't be the same as the name of the containing
                    // type.
                    if ((typeKind == TypeKind.Class || typeKind == TypeKind.Struct)
                        && containingNamespaceOrTypeSymbol.Name == name)
                    {
                        return false;
                    }
                }

                // The name can't be the same as the name of an other member of the same type. At this point no special
                // consideration is given to overloaded methods.
                ImmutableArray<ISymbol> siblings = containingNamespaceOrTypeSymbol.GetMembers(name);
                if (!siblings.IsDefaultOrEmpty)
                {
                    return false;
                }

                return true;
            }
            else if (containingSymbol.Kind == SymbolKind.Method)
            {
                IMethodSymbol methodSymbol = (IMethodSymbol)containingSymbol;
                if (methodSymbol.Parameters.Any(i => i.Name == name)
                    || methodSymbol.TypeParameters.Any(i => i.Name == name))
                {
                    return false;
                }

                IMethodSymbol outermostMethod = methodSymbol;
                while (outermostMethod.ContainingSymbol.Kind == SymbolKind.Method)
                {
                    outermostMethod = (IMethodSymbol)outermostMethod.ContainingSymbol;
                    if (outermostMethod.Parameters.Any(i => i.Name == name)
                        || outermostMethod.TypeParameters.Any(i => i.Name == name))
                    {
                        return false;
                    }
                }

                foreach (var syntaxReference in outermostMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode syntaxNode = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    LocalNameFinder localNameFinder = new LocalNameFinder(name);
                    localNameFinder.Visit(syntaxNode);
                    if (localNameFinder.Found)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        public static SyntaxNode GetParentDeclaration(SyntaxToken token)
        {
            SyntaxNode parent = token.Parent;

            while (parent != null)
            {
                switch (parent.Kind())
                {
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.Parameter:
                case SyntaxKind.TypeParameter:
                case SyntaxKind.CatchDeclaration:
                case SyntaxKind.ExternAliasDirective:
                case SyntaxKind.QueryContinuation:
                case SyntaxKind.FromClause:
                case SyntaxKind.LetClause:
                case SyntaxKind.JoinClause:
                case SyntaxKind.JoinIntoClause:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.UsingDirective:
                case SyntaxKind.LabeledStatement:
                case SyntaxKind.AnonymousObjectMemberDeclarator:
                    return parent;

                default:
                    var declarationParent = parent as MemberDeclarationSyntax;
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

        private class LocalNameFinder : CSharpSyntaxWalker
        {
            private readonly string name;

            public LocalNameFinder(string name)
            {
                this.name = name;
            }

            public bool Found
            {
                get;
                private set;
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitVariableDeclarator(node);
            }

            public override void VisitParameter(ParameterSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitParameter(node);
            }

            public override void VisitTypeParameter(TypeParameterSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitTypeParameter(node);
            }

            public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitCatchDeclaration(node);
            }

            public override void VisitQueryContinuation(QueryContinuationSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitQueryContinuation(node);
            }

            public override void VisitFromClause(FromClauseSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitFromClause(node);
            }

            public override void VisitLetClause(LetClauseSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitLetClause(node);
            }

            public override void VisitJoinClause(JoinClauseSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitJoinClause(node);
            }

            public override void VisitJoinIntoClause(JoinIntoClauseSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitJoinIntoClause(node);
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitForEachStatement(node);
            }

            public override void VisitLabeledStatement(LabeledStatementSyntax node)
            {
                this.Found |= node.Identifier.ValueText == this.name;
                base.VisitLabeledStatement(node);
            }

            public override void VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
            {
                this.Found |= node.NameEquals?.Name?.Identifier.ValueText == this.name;
                base.VisitAnonymousObjectMemberDeclarator(node);
            }
        }
    }
}
