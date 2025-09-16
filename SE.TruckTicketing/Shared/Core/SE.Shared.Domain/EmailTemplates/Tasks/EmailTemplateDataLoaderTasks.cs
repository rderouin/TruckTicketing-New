using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.EmailTemplates.Tasks;

public class EmailTemplateDataLoaderTasks : WorkflowTaskBase<BusinessContext<EmailTemplateEntity>>
{
    private readonly IProvider<Guid, EmailTemplateEntity> _emailTemplateProvider;

    public EmailTemplateDataLoaderTasks(IProvider<Guid, EmailTemplateEntity> emailTemplateProvider)
    {
        _emailTemplateProvider = emailTemplateProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<EmailTemplateEntity> context)
    {
        await LoadUniqueFlag(context.Target);
        await LoadSiblingTemplates(context.Target);

        return true;
    }

    private async Task LoadUniqueFlag(EmailTemplateEntity entity)
    {
        var duplicates = await _emailTemplateProvider.Get(template => template.Name == entity.Name &&
                                                                      template.Id != entity.Id);

        entity.HasUniqueName = !duplicates.Any();
    }

    private async Task LoadSiblingTemplates(EmailTemplateEntity entity)
    {
        var siblings = await _emailTemplateProvider.Get(template => template.EventId == entity.EventId &&
                                                                    template.Id != entity.Id);

        entity.Siblings = siblings.ToArray();
    }

    public override Task<bool> ShouldRun(BusinessContext<EmailTemplateEntity> context)
    {
        return Task.FromResult(true);
    }
}
