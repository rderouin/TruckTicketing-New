using System;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Configuration;

public class NavigationConfigurationManager : ManagerBase<Guid, NavigationConfigurationEntity>
{
    public NavigationConfigurationManager(ILog logger,
                                          IProvider<Guid, NavigationConfigurationEntity> provider,
                                          IValidationManager<NavigationConfigurationEntity> validationManager = null,
                                          IWorkflowManager<NavigationConfigurationEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
    }
}
