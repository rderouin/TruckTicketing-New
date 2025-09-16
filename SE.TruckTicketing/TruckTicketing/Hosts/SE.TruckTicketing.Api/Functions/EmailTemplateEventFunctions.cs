using System;

using SE.Shared.Domain.EmailTemplates;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.EmailTemplateEvents.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.EmailTemplateEvents.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
public partial class EmailTemplateEventFunctions : HttpFunctionApiBase<EmailTemplateEvent, EmailTemplateEventEntity, Guid>
{
    public EmailTemplateEventFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, EmailTemplateEventEntity> manager) : base(log, mapper, manager)
    {
    }
}
