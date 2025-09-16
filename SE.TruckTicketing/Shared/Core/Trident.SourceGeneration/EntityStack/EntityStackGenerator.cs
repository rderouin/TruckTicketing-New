using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Trident.SourceGeneration.EntityStack;

[Generator]
public class EntityStackGenerator : IIncrementalGenerator
{
    private const string GenerateManagerAttributeName = "Trident.SourceGeneration.Attributes.GenerateManagerAttribute";

    private const string GenerateProviderAttributeName = "Trident.SourceGeneration.Attributes.GenerateProviderAttribute";

    private const string GenerateRepositoryAttributeName = "Trident.SourceGeneration.Attributes.GenerateRepositoryAttribute";

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
            var path = Path.Combine(Path.GetTempPath(), Path.ChangeExtension("Source-Gen-Log-EStack-" + DateTime.Today.ToShortDateString(), ".txt"));
            File.AppendAllLines(path, new[] { e.ToString() });
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var classToGenerate in GetClassesToGenerate(compilation, classes, context.CancellationToken))
        {
            context.AddSource(classToGenerate.FileName,
                              SourceText.From(classToGenerate.Type switch
                                              {
                                                  EntityStackClassType.Manager => classToGenerate.GenerateManagerClass(),
                                                  EntityStackClassType.Provider => classToGenerate.GenerateProviderClass(),
                                                  EntityStackClassType.Repository => classToGenerate.GenerateRepositoryClass(),
                                                  _ => throw new NotImplementedException(),
                                              }, Encoding.UTF8));
        }
    }

    private static List<EntityStackClassToGenerate> GetClassesToGenerate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, CancellationToken cancellationToken)
    {
        var classesToGenerate = new List<EntityStackClassToGenerate>();
        var generateManagerAttribute = compilation.GetTypeByMetadataName(GenerateManagerAttributeName);
        var generateProvideAttribute = compilation.GetTypeByMetadataName(GenerateProviderAttributeName);
        var generateRepositoryAttribute = compilation.GetTypeByMetadataName(GenerateRepositoryAttributeName);

        if (generateManagerAttribute is null ||
            generateProvideAttribute is null ||
            generateRepositoryAttribute is null)
        {
            return classesToGenerate;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            foreach (var attribute in typeSymbol.GetAttributes())
            {
                var entityTypeName = typeSymbol.Name.Replace("Entity", "");
                var entityIdType = GetIdType(typeSymbol);

                if (attribute.AttributeClass.Equals(generateManagerAttribute, SymbolEqualityComparer.Default))
                {
                    classesToGenerate.Add(new(entityTypeName + "Manager.g.cs",
                                              typeSymbol.ContainingNamespace.ToDisplayString(),
                                              entityTypeName + "Manager",
                                              EntityStackClassType.Manager,
                                              typeSymbol.Name, 
                                              entityIdType));
                }

                if (attribute.AttributeClass.Equals(generateProvideAttribute, SymbolEqualityComparer.Default))
                {
                    classesToGenerate.Add(new(entityTypeName + "Provider.g.cs",
                                              typeSymbol.ContainingNamespace.ToDisplayString(),
                                              entityTypeName + "Provider",
                                              EntityStackClassType.Provider,
                                              typeSymbol.Name, 
                                              entityIdType));
                }

                if (attribute.AttributeClass.Equals(generateRepositoryAttribute, SymbolEqualityComparer.Default))
                {
                    classesToGenerate.Add(new(entityTypeName + "Repository.g.cs",
                                              typeSymbol.ContainingNamespace.ToDisplayString(),
                                              entityTypeName + "Repository",
                                              EntityStackClassType.Repository,
                                              typeSymbol.Name, 
                                              entityIdType));
                }
            }
        }

        return classesToGenerate;
    }

    private static string GetIdType(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.MetadataName == "EntityBase`1")
        {
            return typeSymbol.TypeArguments.First().Name;
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

                if (attributeTypeSymbolName == GenerateManagerAttributeName ||
                    attributeTypeSymbolName == GenerateProviderAttributeName ||
                    attributeTypeSymbolName == GenerateRepositoryAttributeName)
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
