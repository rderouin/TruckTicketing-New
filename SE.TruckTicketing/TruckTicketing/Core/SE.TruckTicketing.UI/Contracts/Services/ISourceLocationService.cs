using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ISourceLocationService : IServiceBase<SourceLocation, Guid>
{
    Task<Response<SourceLocation>> MarkSourceLocationDeleted(Guid sourceLocationId);
}
