using System;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Facilities;

public class FacilityManager : ManagerBase<Guid, FacilityEntity>
{
    public FacilityManager(ILog logger,
                           IProvider<Guid, FacilityEntity> provider,
                           IValidationManager<FacilityEntity> validationManager = null,
                           IWorkflowManager<FacilityEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
    }
}
