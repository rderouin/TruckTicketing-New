using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ITradeAgreementUploadService : IServiceProxyBase<TradeAgreementUpload, Guid>
{
    Task<TradeAgreementUpload> GetUploadUrl();
}
