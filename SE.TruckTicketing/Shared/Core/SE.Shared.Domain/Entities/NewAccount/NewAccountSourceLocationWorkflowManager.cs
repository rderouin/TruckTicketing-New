using System.Threading.Tasks;
using SE.Shared.Domain.Entities.SourceLocation;
using Trident.Business;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.NewAccount;

public class NewAccountSourceLocationWorkflowManager : INewAccountSourceLocationWorkflowManager
{
    private readonly ILog _logger;

    private readonly IValidationManager _validationManager;

    private readonly IWorkflowManager _workflowManager;

    public NewAccountSourceLocationWorkflowManager(ILog logger,
                                                   IValidationManager<SourceLocationEntity> sourceLocationValidationManager = null,
                                                   IWorkflowManager<SourceLocationEntity> sourceLocationWorkflowManager = null)
    {
        _logger = logger;
        _validationManager = sourceLocationValidationManager ?? new DefaultValidationManager<SourceLocationEntity>(null);
        _workflowManager = sourceLocationWorkflowManager ?? new DefaultWorkflowManager<SourceLocationEntity>(null, logger);
    }

    public async Task RunSourceLocationWorkflowValidation(SourceLocationEntity sourceLocation)
    {
        var context = await CreateBusinessContext(Operation.Insert, sourceLocation);
        await _workflowManager?.Run(context, OperationStage.BeforeInsert);

        await _validationManager?.Validate(context);
    }

    protected virtual Task<BusinessContext<SourceLocationEntity>> CreateBusinessContext(Operation operation, SourceLocationEntity entity)
    {
        return Task.FromResult(CreateBusinessContextSync(operation, entity, null));
    }

    protected virtual BusinessContext<SourceLocationEntity> CreateBusinessContextSync(Operation operation, SourceLocationEntity entity, SourceLocationEntity original)
    {
        return new(entity, original)
        {
            Operation = operation,
        };
    }
}
