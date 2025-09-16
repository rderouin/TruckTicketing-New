using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public interface ISalesLineWatcher : ITruckTicketWorkflow
{
    Task<ICollection<SalesLine>> LoadExistingSalesLines(TruckTicketExperienceViewModel viewModel);

    string[] GetDependencies(TruckTicketExperienceViewModel viewModel);

    ICollection<SalesLine> DeduplicateSalesLines(ICollection<SalesLine> salesLines);

    SalesLinePreviewRequest GenerateSalesLinePreviewRequest(TruckTicketExperienceViewModel viewModel);
}

public class SalesLinesWatcher : ComponentBase, ISalesLineWatcher
{
    public string[] _dependencies = Array.Empty<string>();

    [Inject]
    private ISalesLineService SalesLineService { get; set; }

    public async Task<ICollection<SalesLine>> LoadExistingSalesLines(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        if (!truckTicket.TicketNumber.HasText() || !truckTicket.SalesLineIds.Any())
        {
            return Array.Empty<SalesLine>();
        }

        var search = await SalesLineService.Search(new()
        {
            Filters = new()
            {
                [nameof(SalesLine.TruckTicketId)] = truckTicket.Id,
                [nameof(SalesLine.Status)] = new CompareModel
                {
                    Operator = CompareOperators.ne,
                    Value = SalesLineStatus.Void.ToString(),
                },
            },
        });

        return search.Results.ToArray();
    }

    public string[] GetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket.Clone();
        return new[]
        {
            truckTicket.Id.ToString(),
            truckTicket.BillingCustomerId.ToString(),
            truckTicket.LoadDate.ToString(),
            truckTicket.LoadVolume?.ToString("N2"),
            truckTicket.TotalVolume.ToString("N2"),
            truckTicket.NetWeight.ToString("N2"),
            truckTicket.ServiceType,
        };
    }

    public ICollection<SalesLine> DeduplicateSalesLines(ICollection<SalesLine> salesLines)
    {
        if (salesLines is not null && salesLines.All(salesLine => salesLine.Status is SalesLineStatus.Preview or SalesLineStatus.Exception))
        {
            return salesLines.DistinctBy(salesLine => salesLine.ProductNumber).ToList();
        }

        return salesLines;
    }

    public async ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = Array.Empty<string>();

        if (!viewModel.IsRefresh)
        {
            viewModel.IsLoadingSalesLines = true;
            viewModel.TriggerStateChanged();
            var existingSalesLines = await LoadExistingSalesLines(viewModel);
            existingSalesLines = DeduplicateSalesLines(existingSalesLines);
            viewModel.IsLoadingSalesLines = false;
            viewModel.TriggerStateChanged();

            var reversedSalesLines = existingSalesLines.Where(salesLine => salesLine.IsReversal || salesLine.IsReversed).ToList();
            if (reversedSalesLines.Any())
            {
                viewModel.SetReversedSalesLines(reversedSalesLines);
            }

            var activeSalesLines = existingSalesLines.Except(reversedSalesLines).ToList();

            if (activeSalesLines.Any())
            {
                await viewModel.SetSalesLines(activeSalesLines, true);
                _dependencies = GetDependencies(viewModel);
                return;
            }
        }

        //Add UserAddedAdditionalService from reversedSalesLines if exist
        var persistedAdditionalServices = viewModel.ReversedSalesLines.Where(x => x.IsUserAddedAdditionalServices && x.IsReversed).ToList();
        if (persistedAdditionalServices.Any())
        {
            async void AddPersistedAdditionalService(SalesLine line)
            {
                await viewModel.AddAdditionalServiceSalesLine(line.ProductName, line.ProductNumber, line.UnitOfMeasure, line.Quantity, true);
            }

            persistedAdditionalServices.ForEach(AddPersistedAdditionalService);
        }

        if (await ShouldRun(viewModel))
        {
            await Run(viewModel);
        }
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var dependencies = GetDependencies(viewModel);

        var hasScaleLoad = truckTicket.NetWeight != 0 &&
                           truckTicket.GrossWeight != 0 &&
                           truckTicket.TareWeight != 0;

        var hasFluidLoad = truckTicket.LoadVolume.GetValueOrDefault(0) != 0 &&
                           truckTicket.TotalVolume != 0 &&
                           Math.Abs(truckTicket.TotalVolume - (truckTicket.LoadVolume ?? 0)) < 0.011;

        var hasLoad = truckTicket.FacilityType == FacilityType.Lf
                          ? hasScaleLoad
                          : hasFluidLoad;

        var isServiceOnly = truckTicket.IsServiceOnlyTicket == true;

        var shouldRun = truckTicket.BillingConfigurationId != Guid.Empty &&
                        (hasLoad || isServiceOnly) &&
                        !_dependencies.SequenceEqual(dependencies);

        _dependencies = dependencies;
        return ValueTask.FromResult(shouldRun);
    }

    public async ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel)
    {
        viewModel.IsLoadingSalesLines = true;
        viewModel.TriggerStateChanged();
        var salesLines = await LoadPreviewSalesLines(viewModel);
        viewModel.IsLoadingSalesLines = false;
        viewModel.TriggerStateChanged();
        await viewModel.SetSalesLines(salesLines);
        return true;
    }

    public SalesLinePreviewRequest GenerateSalesLinePreviewRequest(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;

        return new()
        {
            TruckTicket = truckTicket,
            BillingCustomerId = truckTicket.BillingCustomerId,
            FacilityId = truckTicket.FacilityId,
            FacilityServiceSubstanceIndexId = truckTicket.FacilityServiceSubstanceId ?? Guid.Empty,
            WellClassification = truckTicket.WellClassification,
            LoadDate = truckTicket.LoadDate,
            SourceLocationId = truckTicket.SourceLocationId,
            GrossWeight = truckTicket.GrossWeight,
            TareWeight = truckTicket.TareWeight,
            TotalVolume = truckTicket.TotalVolume,
            TotalVolumePercent = truckTicket.TotalVolumePercent,
            OilVolume = truckTicket.OilVolume,
            OilVolumePercent = truckTicket.OilVolumePercent,
            WaterVolume = truckTicket.WaterVolume,
            WaterVolumePercent = truckTicket.WaterVolumePercent,
            SolidVolume = truckTicket.SolidVolume,
            SolidVolumePercent = truckTicket.SolidVolumePercent,
            MaterialApprovalId = truckTicket.MaterialApprovalId,
            MaterialApprovalNumber = truckTicket.MaterialApprovalNumber,
            ServiceTypeId = truckTicket.ServiceTypeId,
            ServiceTypeName = truckTicket.ServiceType,
            TruckingCompanyId = truckTicket.TruckingCompanyId,
            TruckingCompanyName = truckTicket.TruckingCompanyName,
        };
    }

    private async Task<ICollection<SalesLine>> LoadExistingReversedSalesLines(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        if (!truckTicket.TicketNumber.HasText())
        {
            return Array.Empty<SalesLine>();
        }

        var search = await SalesLineService.Search(new()
        {
            Filters = new()
            {
                [nameof(SalesLine.TruckTicketId)] = truckTicket.Id,
            },
        });

        return search.Results.ToArray();
    }

    private async Task<ICollection<SalesLine>> LoadPreviewSalesLines(TruckTicketExperienceViewModel viewModel)
    {
        if (viewModel.BillingCustomerId == Guid.Empty)
        {
            return new List<SalesLine>();
        }

        var previewSalesLineRequest = GenerateSalesLinePreviewRequest(viewModel);
        var salesLines = await SalesLineService.GetPreviewSalesLines(previewSalesLineRequest);
        return salesLines;
    }
}
