namespace Trident.SourceGeneration.EntityStack;

internal static class EntityStackClassSourceBuilderExtensions
{
    public static string GenerateManagerClass(this EntityStackClassToGenerate classToGenerate)
    {
        return string.Format(@"
            using System;
            using Trident.Business;
            using Trident.Data.Contracts;
            using Trident.Validation;
            using Trident.Workflow;
            using Trident.Logging;

            namespace {0}
            {{
                [System.CodeDom.Compiler.GeneratedCode(""Trident.SourceGeneration"", ""1.0.0"")]
                public class {1} : ManagerBase<{3}, {2}>
                {{
                    public {1}(
                        ILog logger,
                        IProvider<{3}, {2}> provider,
                        IValidationManager<{2}> validationManager = null,
                        IWorkflowManager<{2}> workflowManager = null) : base(logger, provider, validationManager, workflowManager)
                    {{
                    }}
                }}
            }}
        ", classToGenerate.NamespaceName, classToGenerate.ClassName, classToGenerate.EntityClassName, classToGenerate.EntityIdType);
    }

    public static string GenerateProviderClass(this EntityStackClassToGenerate classToGenerate)
    {
        return string.Format(@"
            using System;
            using Trident.Business;
            using Trident.Search;

            namespace {0}
            {{
                [System.CodeDom.Compiler.GeneratedCode(""Trident.SourceGeneration"", ""1.0.0"")]
                public class {1} : ProviderBase<{3}, {2}>
                {{
                    public {1}(ISearchRepository<{2}> repository) : base(repository)
                    {{
                    }}
                }}
            }}
        ", classToGenerate.NamespaceName, classToGenerate.ClassName, classToGenerate.EntityClassName, classToGenerate.EntityIdType);
    }

    public static string GenerateRepositoryClass(this EntityStackClassToGenerate classToGenerate)
    {
        return string.Format(@"
            using Trident.Data.Contracts;
            using Trident.EFCore;
            using Trident.Search;

            namespace {0}
            {{
                [System.CodeDom.Compiler.GeneratedCode(""Trident.SourceGeneration"", ""1.0.0"")]
                public class {1} : CosmosEFCoreSearchRepositoryBase<{2}>
                {{
                    public {1}(
                        ISearchResultsBuilder resultsBuilder,
                        ISearchQueryBuilder queryBuilder,
                        IAbstractContextFactory abstractContextFactory,
                        IQueryableHelper queryableHelper) : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
                    {{
                    }}
                }}
            }}
        ", classToGenerate.NamespaceName, classToGenerate.ClassName, classToGenerate.EntityClassName);
    }
}
