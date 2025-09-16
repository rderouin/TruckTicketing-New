using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.Shared.Domain.Processors;
using SE.TruckTicketing.Contracts.Api.Models.SpartanData;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Mapper;

namespace SE.TruckTicketing.Domain.Entities.SpartanSummary;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.TruckTicket)]
public class SpartanSummaryMessageProcessor : BaseEntityProcessor<SpartanSummaryModel>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, FacilityServiceEntity> _facilityServiceProvider;

    private readonly IManager<Guid, FacilityServiceSubstanceIndexEntity> _facilityServiceSubstanceManager;

    private readonly ILeaseObjectBlobStorage _leaseManager;

    private readonly IMapperRegistry _mapperRegistry;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly IProvider<Guid, SpartanProductParameterEntity> _spartanProductProvider;

    private readonly IManager<Guid, TruckTicketEntity> _truckTicketManager;

    private FacilityEntity _facilityEntity;

    private List<FacilityServiceEntity> _facilityServicesEntities;

    public SpartanSummaryMessageProcessor(IManager<Guid, TruckTicketEntity> truckTicketManager,
                                          IProvider<Guid, FacilityEntity> facilityProvider,
                                          IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                                          IProvider<Guid, SpartanProductParameterEntity> spartanProductProvider,
                                          IProvider<Guid, FacilityServiceEntity> facilityServiceProvider,
                                          IProvider<Guid, AccountEntity> accountProvider,
                                          IManager<Guid, FacilityServiceSubstanceIndexEntity> facilityServiceSubstanceManager,
                                          IManager<Guid, NoteEntity> noteManager,
                                          IMapperRegistry mapperRegistry,
                                          ILeaseObjectBlobStorage leaseManager)
    {
        _truckTicketManager = truckTicketManager;
        _facilityProvider = facilityProvider;
        _sourceLocationProvider = sourceLocationProvider;
        _spartanProductProvider = spartanProductProvider;
        _facilityServiceProvider = facilityServiceProvider;
        _accountProvider = accountProvider;
        _facilityServiceSubstanceManager = facilityServiceSubstanceManager;
        _noteManager = noteManager;
        _mapperRegistry = mapperRegistry;
        _leaseManager = leaseManager;
    }

    public override async Task Process(EntityEnvelopeModel<SpartanSummaryModel> model)
    {
        var summary = model.Payload;
        var truckTicket = BuildTruckTicket(summary);

        await _leaseManager.AcquireLeaseAndExecute(async () => await ProcessTruckTicket(model, truckTicket), nameof(SpartanSummaryModel) + truckTicket.TicketNumber);
    }

    private async Task<bool> ProcessTruckTicket(EntityEnvelopeModel<SpartanSummaryModel> model, TruckTicketEntity truckTicket)
    {
        var summary = model.Payload;
        var existingTicket = await _truckTicketManager.Get(ticket => ticket.TicketNumber == truckTicket.TicketNumber); // PK - TODO: INT
        if (existingTicket.Any())
        {
            return false;
        }

        await EnrichFacilityData(summary, truckTicket);
        await EnrichSourceLocationData(summary, truckTicket);
        await EnrichTruckingCompanyData(summary, truckTicket);
        var errorMessage = await AssignFacilityServiceAsync(summary, truckTicket);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            var spartanInfo = Environment.NewLine + summary.ToJson();

            var truckTicketNote = new NoteEntity
            {
                Id = Guid.NewGuid(),
                Comment = errorMessage + spartanInfo,
                ThreadId = $"TruckTicket|{truckTicket.Id}",
                CreatedBy = "Spartan Message Processor",
                UpdatedBy = "Spartan Message Processor",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await _noteManager.Save(truckTicketNote, true);
        }

        await _truckTicketManager.Save(truckTicket);
        return true;
    }

    private TruckTicketEntity BuildTruckTicket(SpartanSummaryModel summary)
    {
        var ticket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Source = TruckTicketSource.Spartan,
            TruckTicketType = TruckTicketType.SP,
            ValidationStatus = TruckTicketValidationStatus.Valid,
            Status = TruckTicketStatus.Open,
            TicketNumber = String.Concat(summary.PlantIdentifier, summary.CustomerTransactionId, "-SP"),
            CreatedBy = "Spartan Message Processor",
            UpdatedBy = "Spartan Message Processor",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _mapperRegistry.Map(summary, ticket);

        ticket.WaterVolumePercent = GetPercentageValue(ticket.WaterVolume, ticket.TotalVolume);
        ticket.OilVolumePercent = GetPercentageValue(ticket.OilVolume, ticket.TotalVolume);
        ticket.SolidVolumePercent = GetPercentageValue(ticket.SolidVolume, ticket.TotalVolume);

        ticket.UnloadOilDensity = Math.Round(ticket.UnloadOilDensity, 1);

        if (ticket.WaterVolumePercent.Eq(100.0, 0.0001))
        {
            ticket.UnloadOilDensity = 0;
        }

        ticket.TotalVolumePercent = ticket.WaterVolumePercent + ticket.OilVolumePercent + ticket.SolidVolumePercent;

        return ticket;
    }

    private double GetPercentageValue(double volume, double totalVolume)
    {
        return totalVolume > 0 ? Math.Round(volume * 100.0 / totalVolume, 1) : 0;
    }

    private async Task EnrichFacilityData(SpartanSummaryModel summary, TruckTicketEntity truckTicket)
    {
        var facility = (await _facilityProvider.Get(facility => facility.SiteId == summary.PlantIdentifier)).FirstOrDefault();
        if (facility is null)
        {
            return;
        }

        truckTicket.FacilityId = facility.Id;
        truckTicket.FacilityName = facility.Name;
        truckTicket.CountryCode = facility.CountryCode;
        truckTicket.LegalEntityId = facility.LegalEntityId;
        truckTicket.LegalEntity = facility.LegalEntity;
        truckTicket.FacilityType = facility.Type;
        truckTicket.FacilityLocationCode = facility.LocationCode;
        truckTicket.SiteId = facility.SiteId;
        _facilityServicesEntities = (await _facilityServiceProvider.Get(fs => fs.FacilityId == facility.Id))?.ToList() ?? new();
        _facilityEntity = facility;
    }

    private async Task EnrichTruckingCompanyData(SpartanSummaryModel summary, TruckTicketEntity truckTicket)
    {
        var account = (await _accountProvider.Get(account => account.Name == summary.TransportCompanyName && account.AccountTypes.Raw.Contains(AccountTypes.TruckingCompany))).FirstOrDefault();
        if (account is null)
        {
            return;
        }

        truckTicket.TruckingCompanyId = account.Id;
        truckTicket.TruckingCompanyName = account.Name;
    }

    private async Task EnrichSourceLocationData(SpartanSummaryModel summary, TruckTicketEntity truckTicket)
    {
        var sourceLocation = (await _sourceLocationProvider.Get(location => location.FormattedIdentifier == summary.LocationUwi)).FirstOrDefault();
        if (sourceLocation is null)
        {
            return;
        }

        truckTicket.SourceLocationId = sourceLocation.Id;
        truckTicket.SourceLocationName = sourceLocation.SourceLocationName;
        truckTicket.SourceLocationFormatted = sourceLocation.FormattedIdentifier;
        truckTicket.SourceLocationUnformatted = sourceLocation.Identifier;
        truckTicket.SourceLocationCode = sourceLocation.SourceLocationCode;
        truckTicket.GeneratorId = sourceLocation.GeneratorId;
        truckTicket.GeneratorName = sourceLocation.GeneratorName;
    }

    private async Task<string> AssignFacilityServiceAsync(SpartanSummaryModel summary, TruckTicketEntity truckTicket)
    {
        if (_facilityServicesEntities == null)
        {
            return string.Empty;
        }

        var spartanProductParamIds = _facilityServicesEntities.SelectMany(facilityService => facilityService.SpartanProductParameters.Select(spartanParam => spartanParam.SpartanProductParameterId))
                                                              .Distinct()
                                                              .ToArray();

        var spartanProducts = (await _spartanProductProvider.GetByIds(spartanProductParamIds)).ToDictionary(spartanProduct => spartanProduct.Id);

        var spartanProductParams = _facilityServicesEntities.SelectMany(facilityService => facilityService.SpartanProductParameters
                                                                                                          .Select(spartanProductParameter =>
                                                                                                                  {
                                                                                                                      spartanProducts.TryGetValue(spartanProductParameter.SpartanProductParameterId,
                                                                                                                          out var spartanProduct);

                                                                                                                      return (spartanProduct, facilityService);
                                                                                                                  }))
                                                            .Where(fsp => fsp.spartanProduct is not null)
                                                            .ToArray();

        if (spartanProductParams.Length == 0)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.NoSpartanProductInFacilityService.Replace("[Spartan Product]", summary.ProductName);
        }

        spartanProductParams = spartanProductParams.Where(fsp => fsp.spartanProduct.ProductName == summary.ProductName).ToArray();
        if (spartanProductParams.Length == 0)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.AssignedSpartanProductNotFound.Replace("[Spartan Product]", summary.ProductName);
        }

        spartanProductParams = spartanProductParams.Where(fsp => fsp.spartanProduct.LocationOperatingStatus == summary.LocationOperatingStatus).ToArray();
        if (spartanProductParams.Length == 0)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.LocationOperatingStatusNotFound.Replace("[Spartan Product]", summary.ProductName);
        }

        spartanProductParams = spartanProductParams.Where(fsp => fsp.spartanProduct.MinWaterPercentage <= summary.RoundedCorrectedEmulsionWaterCut &&
                                                                 fsp.spartanProduct.MaxWaterPercentage >= summary.RoundedCorrectedEmulsionWaterCut)
                                                   .ToArray();

        if (spartanProductParams.Length == 0)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.WaterVolumePercentageNotInRange.Replace("[Cuts (%) - Water]", summary.RoundedCorrectedEmulsionWaterCut.ToString(CultureInfo.CurrentCulture))
                                           .Replace("[Spartan Product]", summary.ProductName);
        }

        var roundedCorrectedOilDensity = Math.Round(summary.CorrectedOilDensity);
        spartanProductParams = spartanProductParams.Where(fsp => fsp.spartanProduct.MinFluidDensity <= roundedCorrectedOilDensity &&
                                                                 fsp.spartanProduct.MaxFluidDensity >= roundedCorrectedOilDensity)
                                                   .ToArray();

        if (spartanProductParams.Length == 0)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.FluidDensityNotInRange.Replace("[ Fluid Density]", summary.CorrectedOilDensity.ToString(CultureInfo.InvariantCulture))
                                           .Replace("[Spartan Product]", summary.ProductName);
        }

        if (spartanProductParams.Length > 1)
        {
            truckTicket.ValidationStatus = TruckTicketValidationStatus.Error;
            return SpartanDataErrorMessages.MultipleFacilityServiceFound.Replace("[Spartan Product]", summary.ProductName);
        }

        var (spartanProductParam, facilityService) = spartanProductParams.First();
        var facilityServiceSubstances =
            (await _facilityServiceSubstanceManager.Get(s => s.FacilityId == truckTicket.FacilityId && s.FacilityServiceId == facilityService.Id && s.IsAuthorized))
           .DistinctBy(service => service.Substance)
           .ToArray();

        truckTicket.SpartanProductParameterId = spartanProductParam.Id;
        truckTicket.SpartanProductParameterDisplay =
            $"{(spartanProductParam.FluidIdentity != FluidIdentity.Undefined ? spartanProductParam.FluidIdentity.ToString() : "")}; {spartanProductParam.ProductName}; Density {spartanProductParam.MinFluidDensity:N1} - {spartanProductParam.MaxFluidDensity:N1}; Water {spartanProductParam.MinWaterPercentage:N2} - {spartanProductParam.MaxWaterPercentage:N2}; {(spartanProductParam.LocationOperatingStatus != LocationOperatingStatus.Blank ? spartanProductParam.LocationOperatingStatus.ToString() : "")}";

        var matchedFacilityServiceSubstance = facilityServiceSubstances.Length == 1
                                                  ? facilityServiceSubstances[0]
                                                  : facilityServiceSubstances.FirstOrDefault(service => service.Substance?.Trim()
                                                                                                               .Equals(spartanProductParam.ProductName?.Trim(), StringComparison.OrdinalIgnoreCase) ??
                                                                                                        false);

        if (matchedFacilityServiceSubstance is not null)
        {
            AssignFacilityServiceFacilitySubstance(truckTicket, matchedFacilityServiceSubstance);
        }
        else
        {
            truckTicket.EnforceSpartanFacilityServiceLock = true;
            truckTicket.LockedSpartanFacilityServiceId = facilityService.Id;
            truckTicket.LockedSpartanFacilityServiceName = facilityService.ServiceTypeName;
            return SpartanDataErrorMessages.MultipleFacilityServiceSubstancesFound;
        }

        return string.Empty;
    }

    private void AssignFacilityServiceFacilitySubstance(TruckTicketEntity truckTicket, FacilityServiceSubstanceIndexEntity facilityServiceSubstance)
    {
        truckTicket.FacilityServiceSubstanceId = facilityServiceSubstance?.Id ?? Guid.Empty;
        truckTicket.SubstanceId = facilityServiceSubstance?.SubstanceId ?? Guid.Empty;
        truckTicket.SubstanceName = facilityServiceSubstance?.Substance;
        truckTicket.WasteCode = facilityServiceSubstance?.WasteCode;
        truckTicket.ServiceTypeId = facilityServiceSubstance?.ServiceTypeId;
        truckTicket.ServiceType = facilityServiceSubstance?.ServiceTypeName;
        truckTicket.FacilityServiceId = facilityServiceSubstance?.FacilityServiceId;
        truckTicket.UnitOfMeasure = facilityServiceSubstance?.UnitOfMeasure;
        Enum.TryParse<Stream>(facilityServiceSubstance?.Stream, out var stream);
        truckTicket.Stream = stream;
        truckTicket.FacilityStreamRegulatoryCode = stream switch
                                                   {
                                                       Stream.Water => _facilityEntity?.Water,
                                                       Stream.Waste => _facilityEntity?.Waste,
                                                       Stream.Terminalling => _facilityEntity?.Terminaling,
                                                       Stream.Pipeline => _facilityEntity?.Pipeline,
                                                       Stream.Treating => _facilityEntity?.Treating,
                                                       _ => "",
                                                   };
    }
}

public static class SpartanDataErrorMessages
{
    public const string NoSpartanProductInFacilityService =
        "No Facility Services found with assigned Spartan Product Parameters: [Spartan Product]. Please check the Facility Services for the Ticket Facility.";

    public const string AssignedSpartanProductNotFound =
        "No assigned Spartan Product Parameter found for the Ticket’s Spartan Product: [Spartan Product]. Please check the assigned Spartan Product Parameters. for the Facility Services on the Facility.";

    public const string LocationOperatingStatusNotFound =
        "No assigned Spartan Product Parameter found for the Ticket’s Spartan Product: [Spartan Product]. Please check the assigned Spartan Product Parameters. for the Facility Services on the Facility.";

    public const string WaterVolumePercentageNotInRange =
        "No assigned Spartan Product Parameter found with a water percentage range that accepts the Ticket’s Cut (%) for water: [Cuts (%) - Water]. Please check the assigned Spartan Product Parameters for the Ticket’s Spartan Product: [Spartan Product]. for the Facility Services on the Facility.";

    public const string FluidDensityNotInRange =
        "No assigned Spartan Product Parameter found with a Fluid Density range that fits the Ticket’s Fluid Density: [ Fluid Density]. Please check the assigned Spartan Product Parameters for the Ticket’s Spartan Product: [Spartan Product]. for the Facility Services on the Facility.";

    public const string MultipleFacilityServiceFound =
        "Multiple Facility Services are available for the Ticket’s Spartan Product: [Spartan Product]. One must be selected before the ticket will be Valid.";

    public const string MultipleFacilityServiceSubstancesFound =
        "A facility service was found for the Spartan Ticket. However, multiple substances exist for the facility service, of which one must be selected manually.";
}
