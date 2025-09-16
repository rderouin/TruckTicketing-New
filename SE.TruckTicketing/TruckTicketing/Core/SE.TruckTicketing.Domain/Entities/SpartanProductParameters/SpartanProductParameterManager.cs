using System;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

public class SpartanProductParameterManager : ManagerBase<Guid, SpartanProductParameterEntity>
{
    public SpartanProductParameterManager(ILog logger,
                                          IProvider<Guid, SpartanProductParameterEntity> provider,
                                          IValidationManager<SpartanProductParameterEntity> validationManager = null,
                                          IWorkflowManager<SpartanProductParameterEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
    }
}
