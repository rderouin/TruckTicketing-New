using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IMaterialApprovalService : IServiceBase<MaterialApproval, Guid>
{
    Task<Response<object>> DownloadMaterialApprovalPdf(Guid materialApprovalId);

    Task<Response<object>> DownloadMaterialApprovalScaleTicket(Guid materialApprovalId);
}
