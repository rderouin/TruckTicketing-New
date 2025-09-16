using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.Sampling;

public interface ILandfillSamplingStatusCheckManager : IManager
{
    public Task<LandfillSamplingStatusCheckDto> GetSamplingStatus(LandfillSamplingStatusCheckRequestDto statusCheckRequest);
}

public class LandfillSamplingStatusCheckManager : ILandfillSamplingStatusCheckManager
{
    private static readonly Dictionary<string, string> RawThresholdMessages = new()
    {
        { SamplingRuleType.Load.ToString(), "Max loads before sample must be taken: {threshold}.  Current loads: {value}." },
        { SamplingRuleType.Time.ToString(), "Threshold: Once every {threshold} days.  Last sample taken on: {value}." },
        { SamplingRuleType.Weight.ToString(), "Max weight (tons) before sample must be taken: {threshold}.  Current weight: {value}." },
    };

    private readonly IManager<Guid, FacilityServiceSubstanceIndexEntity> _facilityServiceSubstanceManager;

    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;

    private readonly IManager<Guid, ProductEntity> _productManager;

    public LandfillSamplingStatusCheckManager(IManager<Guid, LandfillSamplingEntity> landfillSamplingManager,
                                              IManager<Guid, ProductEntity> productManager,
                                              IManager<Guid, FacilityServiceSubstanceIndexEntity> facilityServiceSubstanceManager)
    {
        _landfillSamplingManager = landfillSamplingManager;
        _productManager = productManager;
        _facilityServiceSubstanceManager = facilityServiceSubstanceManager;
    }

    public async Task<LandfillSamplingStatusCheckDto> GetSamplingStatus(LandfillSamplingStatusCheckRequestDto statusCheckRequest)
    {
        var samplings = await _landfillSamplingManager.Get(s
                                                               => s.FacilityId == statusCheckRequest.FacilityId);

        var landfillSamplingEntities = samplings.ToList();
        var potentialTruckTicket = new TruckTicketEntity
        {
            NetWeight = statusCheckRequest.NetWeight,
            FacilityServiceSubstanceId = statusCheckRequest.FacilityServiceSubstanceId ?? Guid.Empty,
            FacilityId = statusCheckRequest.FacilityId,
            WellClassification = statusCheckRequest.WellClassification,
        };

        var samplingDto = await ProcessSamplings(landfillSamplingEntities, potentialTruckTicket);

        return samplingDto;
    }

    private async Task<LandfillSamplingStatusCheckDto> ProcessSamplings(List<LandfillSamplingEntity> landfillSamplingEntities,
                                                                        TruckTicketEntity potentialTruckTicket)
    {
        var facilityServiceSubstanceIndex = await _facilityServiceSubstanceManager.GetById(potentialTruckTicket.FacilityServiceSubstanceId);
        var product = await _productManager.GetById(facilityServiceSubstanceIndex?.TotalProductId);
        var productNumber = product?.Number ?? "0";

        var blockResult = EvaluateSamplings(landfillSamplingEntities,
                                            potentialTruckTicket,
                                            productNumber,
                                            LandfillSamplingStatusCheckAction.Block);

        if (blockResult != null)
        {
            return blockResult;
        }

        var warnResult = EvaluateSamplings(landfillSamplingEntities,
                                           potentialTruckTicket,
                                           productNumber,
                                           LandfillSamplingStatusCheckAction.Warn);

        if (warnResult != null)
        {
            return warnResult;
        }

        return new()
        {
            Action = LandfillSamplingStatusCheckAction.Allow,
            Message = null,
        };
    }

    private LandfillSamplingStatusCheckDto EvaluateSamplings(List<LandfillSamplingEntity> landfillSamplingEntities,
                                                             TruckTicketEntity truckTicket,
                                                             string productNumber,
                                                             string action)
    {
        foreach (var sampling in landfillSamplingEntities)
        {
            var compareValue = action == LandfillSamplingStatusCheckAction.Block ? sampling.Threshold : sampling.WarningThreshold;
            var sampleTrackingStrategy = SampleTrackingStrategy.GetTrackingStrategy(sampling.SamplingRuleType);
            if (!sampleTrackingStrategy.SampleAppliesToTicket(sampling, truckTicket, productNumber))
            {
                continue;
            }

            // check each sampling for threshold
            if (sampleTrackingStrategy.OverCompareValue(sampling, compareValue, truckTicket))
            {
                return GenerateResponseDto(sampling, action);
            }
        }

        return null;
    }

    private static LandfillSamplingStatusCheckDto GenerateResponseDto(LandfillSamplingEntity sampling, string action)
    {
        var thresholdMessage = GenerateThresholdMessage(sampling);

        var message = action == LandfillSamplingStatusCheckAction.Block
                          ? $"Error: Truck Sample is due and must be selected as part of ticket. {thresholdMessage}"
                          : $"Warning: Truck Sample will be required soon. {thresholdMessage}";

        var responseDto = new LandfillSamplingStatusCheckDto
        {
            Action = action,
            Message = message,
        };

        return responseDto;
    }

    private static string GenerateThresholdMessage(LandfillSamplingEntity sampling)
    {
        var rawThresholdMessage = RawThresholdMessages[sampling.SamplingRuleType.ToString()];
        var thresholdMessage = rawThresholdMessage
                              .Replace("{value}", sampling.Value)
                              .Replace("{threshold}", sampling.Threshold);

        return thresholdMessage;
    }
}
