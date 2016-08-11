using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using StyleCop.Analyzers;
using StyleCop.Analyzers.Settings.ObjectModel;

namespace AbbreviationFix
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using StyleCop.Analyzers.Helpers;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AbbreviationVBAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AbbreviationAnalyzer";
        private const string Title = "Abbreviations are not allowed except officially registered";
        private const string MessageFormat = "Element '{0}'  must not contain series of capital letters";
        private const string Description = "";
        private const string HelpLink = "";

        private static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, "NamingRules", DiagnosticSeverity.Warning, true, Description, HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(HandleCompilationStart);
        }

        private static void HandleCompilationStart(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(HandleClassStatement, SyntaxKind.ClassStatement);
        }

        private static void HandleClassStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((ClassStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
            Debug.WriteLine(((ClassStatementSyntax)context.Node).Identifier.ValueText);
        }

        private static void CheckElementNameToken(SyntaxNodeAnalysisContext context, SyntaxToken identifier, AbbreviationSettings settings)
        {
            AbbreviationHelper.CheckElementNameToken(context, identifier, settings, Descriptor);
        }
    }
}
