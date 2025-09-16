using System;

using SE.Shared.Domain.Entities.Note;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.Note_BaseRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.Note_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Note_SearchRoute)]
public partial class NoteFunctions : HttpFunctionApiBase<Note, NoteEntity, Guid>
{
    public NoteFunctions(ILog log,
                         IMapperRegistry mapper,
                         IManager<Guid, NoteEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
