using System;

using SE.Shared.Domain.Entities.ServiceType;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.ServiceType;

public class ServiceTypeManager : ManagerBase<Guid, ServiceTypeEntity>
{
    public ServiceTypeManager(ILog logger,
                              IProvider<Guid, ServiceTypeEntity> provider,
                              IValidationManager<ServiceTypeEntity> validationManager = null,
                              IWorkflowManager<ServiceTypeEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
    }
}
