using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Sampling;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ILandfillSamplingService : IServiceBase<LandfillSamplingDto, Guid>
{
    Task<LandfillSamplingStatusCheckDto> CheckStatus(LandfillSamplingStatusCheckRequestDto landfillSamplingStatusCheckRequestDto);
}
