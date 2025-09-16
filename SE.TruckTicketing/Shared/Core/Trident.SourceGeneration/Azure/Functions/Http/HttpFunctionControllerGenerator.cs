using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Trident.SourceGeneration.Azure.Functions.Http;

[Generator]
public class HttpFunctionControllerGenerator : IIncrementalGenerator
{
    private const string UseHttpFunctionAttributeName = "Trident.SourceGeneration.Attributes.UseHttpFunctionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        try
        {
            var classDeclarations = context.SyntaxProvider
                                           .CreateSyntaxProvider(static (syntax, _) => IsSyntaxTargetForGeneration(syntax),
                                                                 static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                                           .Where(static m => m is not null);

            context.RegisterSourceOutput(context.CompilationProvider.Combine(classDeclarations.Collect()),
                                         static (spc, source) => Execute(source.Left, source.Right, spc));
        }
        catch (Exception e)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.ChangeExtension("Source-Gen-Log-Http-" + DateTime.Today.ToShortDateString(), ".txt"));
            File.AppendAllLines(path, new[] { e.ToString() });
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var classToAugment in GetClassesToAugment(compilation, classes, context.CancellationToken))
        {
            context.AddSource($"{classToAugment.ClassName}HttpFunctions.g.cs",
                              SourceText.From(classToAugment.GenerateAugmentedHttpFunctionController(), Encoding.UTF8));
        }
    }

    private static List<HttpControllerClassToAugment> GetClassesToAugment(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, CancellationToken cancellationToken)
    {
        var classesToAugment = new List<HttpControllerClassToAugment>();
        var useHttpFunctionAttribute = compilation.GetTypeByMetadataName(UseHttpFunctionAttributeName);

        if (useHttpFunctionAttribute is null)
        {
            return classesToAugment;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            var attributes = typeSymbol.GetAttributes();
            var functionsToGenerate = new List<HttpFunctionToGenerate>();

            foreach (var attribute in attributes)
            {
                if (!attribute.AttributeClass.Equals(useHttpFunctionAttribute, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                functionsToGenerate.Add(GetHttpFunctionsToGenerate(typeSymbol, attribute));
            }

            classesToAugment.Add(new(typeSymbol.Name, typeSymbol.ContainingNamespace.ToString(), functionsToGenerate));
        }

        return classesToAugment;
    }

    private static HttpFunctionToGenerate GetHttpFunctionsToGenerate(INamedTypeSymbol typeSymbol, AttributeData attribute)
    {
        var httpFunctionType = (HttpFunctionApiMethod)attribute.ConstructorArguments[0].Value;

        var functionName = typeSymbol.Name.Replace("Functions", "") + httpFunctionType;
        var route = functionName;
        var claimsAuthorizeResource = string.Empty;
        var claimsAuthorizeOperation = string.Empty;
        string authorizeFacilityAccessWith = null;
        var entityIdType = GetIdType(typeSymbol);

        if (!attribute.NamedArguments.IsEmpty)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                var typedConstant = argument.Value;

                if (typedConstant.Kind == TypedConstantKind.Error)
                {
                    break;
                }

                switch (argument.Key)
                {
                    case "Route":
                        route = typedConstant.Value.ToString();
                        break;

                    case "ClaimsAuthorizeResource":
                        claimsAuthorizeResource = typedConstant.Value.ToString();
                        break;

                    case "ClaimsAuthorizeOperation":
                        claimsAuthorizeOperation = typedConstant.Value.ToString();
                        break;

                    case "AuthorizeFacilityAccessWith":
                        authorizeFacilityAccessWith = typedConstant.Value.ToString();
                        break;
                }
            }
        }

        return new(httpFunctionType,
                   functionName,
                   route,
                   claimsAuthorizeResource,
                   claimsAuthorizeOperation,
                   authorizeFacilityAccessWith,
                   entityIdType);
    }

    private static string GetIdType(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
        {
            return null;
        }

        if (typeSymbol.MetadataName == "HttpFunctionApiBase`3")
        {
            return typeSymbol.TypeArguments.Last().Name;
        }

        return GetIdType(typeSymbol.BaseType);
    }

    private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeTypeSymbolName = attributeSymbol.ContainingType.ToDisplayString();
                if (attributeTypeSymbolName == UseHttpFunctionAttributeName)
                {
                    return classDeclarationSyntax;
                }
            }
        }

        return null;
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }
}
