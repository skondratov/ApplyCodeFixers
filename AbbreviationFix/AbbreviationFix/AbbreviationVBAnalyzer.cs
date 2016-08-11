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

    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
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
            context.RegisterSyntaxNodeAction(HandleDelegateFunctionStatement, SyntaxKind.DelegateFunctionStatement);
            context.RegisterSyntaxNodeAction(HandleDelegateSubStatement, SyntaxKind.DelegateSubStatement);
            context.RegisterSyntaxNodeAction(HandleEnumMemberDeclaration, SyntaxKind.EnumMemberDeclaration);
            context.RegisterSyntaxNodeAction(HandleEnumStatement, SyntaxKind.EnumStatement);
            context.RegisterSyntaxNodeAction(HandleEventStatement, SyntaxKind.EventStatement);
            context.RegisterSyntaxNodeAction(HandleFunctionStatement, SyntaxKind.FunctionStatement);
            context.RegisterSyntaxNodeAction(HandleInterfaceStatement, SyntaxKind.InterfaceStatement);
            context.RegisterSyntaxNodeAction(HandleModifiedIdentifier, SyntaxKind.ModifiedIdentifier);
            context.RegisterSyntaxNodeAction(HandleModuleStatement, SyntaxKind.ModuleStatement);
            context.RegisterSyntaxNodeAction(HandlePropertyStatement, SyntaxKind.PropertyStatement);
            context.RegisterSyntaxNodeAction(HandleStructureStatement, SyntaxKind.StructureStatement);
            context.RegisterSyntaxNodeAction(HandleSubStatement, SyntaxKind.SubStatement);
        }

        private static void HandleModifiedIdentifier(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((ModifiedIdentifierSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleSubStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((MethodStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleStructureStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((StructureStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandlePropertyStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((PropertyStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleModuleStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((ModuleStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleInterfaceStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((InterfaceStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleFunctionStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((MethodStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleEventStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((EventStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleEnumStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((EnumStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleEnumMemberDeclaration(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((EnumMemberDeclarationSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleDelegateSubStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((DelegateStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleDelegateFunctionStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((DelegateStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void HandleClassStatement(SyntaxNodeAnalysisContext context, StyleCopSettings settings)
        {
            CheckElementNameToken(context, ((ClassStatementSyntax)context.Node).Identifier, settings.AbbreviationRules);
        }

        private static void CheckElementNameToken(SyntaxNodeAnalysisContext context, SyntaxToken identifier, AbbreviationSettings settings)
        {
            AbbreviationHelper.CheckElementNameToken(context, identifier, settings, Descriptor);
        }
    }
}
