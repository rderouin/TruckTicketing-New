using System;

using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Accounts;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.AccountContactReferenceIndex.Id, ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.AccountContactReferenceIndex.Search, ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
public partial class AccountContactReferenceIndexFunctions : HttpFunctionApiBase<AccountContactReferenceIndex, AccountContactReferenceIndexEntity, Guid>
{
    public AccountContactReferenceIndexFunctions(ILog log,
                                                 IMapperRegistry mapper,
                                                 IManager<Guid, AccountContactReferenceIndexEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
