using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;

using Trident.Business;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.NewAccount;

public class NewAccountWorkflowManager : INewAccountWorkflowManager
{
    private readonly ILog _logger;

    private readonly IValidationManager _validationManager;

    private readonly IWorkflowManager _workflowManager;

    public NewAccountWorkflowManager(ILog logger,
                                     IValidationManager<AccountEntity> accountValidationManager = null,
                                     IWorkflowManager<AccountEntity> accountWorkflowManager = null)
    {
        _logger = logger;
        _validationManager = accountValidationManager ?? new DefaultValidationManager<AccountEntity>(null);
        _workflowManager = accountWorkflowManager ?? new DefaultWorkflowManager<AccountEntity>(null, logger);
    }

    public async Task RunAccountWorkflowValidation(AccountEntity account)
    {
        var context = await CreateBusinessContext(Operation.Insert, account);
        await _validationManager?.Validate(context);
    }

    protected virtual Task<BusinessContext<AccountEntity>> CreateBusinessContext(Operation operation, AccountEntity entity)
    {
        return Task.FromResult(CreateBusinessContextSync(operation, entity, null));
    }

    protected virtual BusinessContext<AccountEntity> CreateBusinessContextSync(Operation operation, AccountEntity entity, AccountEntity original)
    {
        return new(entity, original) { Operation = operation };
    }
}
