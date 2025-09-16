using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class LoadConfirmationSignatoryUpdateWithBillingConfigurationTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    public LoadConfirmationSignatoryUpdateWithBillingConfigurationTask(IManager<Guid, LoadConfirmationEntity> loadConfirmationManager)
    {
        _loadConfirmationManager = loadConfirmationManager;
    }

    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        var loadConfirmations = await _loadConfirmationManager.Get(lc => lc.BillingConfigurationId == context.Target.Id // PK - XP for LC by billing config
                                                                      && lc.Status != LoadConfirmationStatus.Posted
                                                                      && lc.Status != LoadConfirmationStatus.Void);

        var loadConfirmationEntities = loadConfirmations.ToList();
        if (!loadConfirmationEntities.Any())
        {
            return true;
        }

        foreach (var loadConfirmationEntity in loadConfirmationEntities)
        {
            var signatories = context.Target.Signatories?.Where(s => s.IsAuthorized).ToList() ?? new();
            loadConfirmationEntity.Signatories = signatories.Clone();
            loadConfirmationEntity.SignatoriesAreUpdated = true;
            await _loadConfirmationManager.Update(loadConfirmationEntity, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Update && IsSignatoryUpdated(context));
    }

    private bool IsSignatoryUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        var original = context.Original?.Signatories.ToJson();
        var target = context.Target.Signatories.ToJson();
        return string.CompareOrdinal(original, target) != 0;
    }
}
