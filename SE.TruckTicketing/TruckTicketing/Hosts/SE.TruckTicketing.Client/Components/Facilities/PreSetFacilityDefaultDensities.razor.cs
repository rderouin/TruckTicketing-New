using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.Facilities;

public partial class PreSetFacilityDefaultDensities : BaseTruckTicketingComponent
{
    private Dictionary<Guid, IEnumerable<Guid>> _facilityServiceMapping = new();

    private PagableGridView<PreSetDensityConversionParams> _grid;

    private SearchResultsModel<PreSetDensityConversionParams, SearchCriteriaModel> _results = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<PreSetDensityConversionParams>(),
    };

    //private IEnumerable<Guid> _facilityService;
    private PreSetDensityConversionParams recordToUpdate;

    private bool IsNew => Facility.Id == Guid.Empty;

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Parameter]
    public Facility Facility { get; set; }

    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public List<PreSetDensityConversionParams> FacilityDefaultDensities { get; set; }

    [Parameter]
    public EventCallback<FacilityService> OnActiveToggle { get; set; }

    private void OnDateStartChange(ref PreSetDensityConversionParams densityRecord, DateTime? setDate)
    {
        if (setDate == null)
        {
            densityRecord.StartDate = default;
            return;
        }

        densityRecord.StartDate = new(setDate.Value.Year, setDate.Value.Month, setDate.Value.Day, 7, 0, 0, TimeSpan.FromHours(-7));
    }

    private void OnDateEndChange(ref PreSetDensityConversionParams densityRecord, DateTime? setDate)
    {
        if (setDate == null)
        {
            densityRecord.EndDate = null;
            return;
        }

        densityRecord.EndDate = new(setDate.Value.Year, setDate.Value.Month, setDate.Value.Day, 7, 0, 0, TimeSpan.FromHours(-7));
    }

    private async Task LoadPreSetFacilityDefaultDensities(SearchCriteriaModel current)
    {
        FacilityDefaultDensities ??= new();
        if (!IsNew && FacilityDefaultDensities.Any())
        {
            foreach (var facilityDensities in FacilityDefaultDensities)
            {
                _facilityServiceMapping.TryAdd(facilityDensities.Id, new List<Guid>());
                if (facilityDensities.FacilityServiceId != null && facilityDensities.FacilityServiceId.Any())
                {
                    _facilityServiceMapping[facilityDensities.Id] = new List<Guid>(facilityDensities.FacilityServiceId);
                }
            }
        }

        var densities = FacilityDefaultDensities;

        if (!string.IsNullOrEmpty(current.Keywords))
        {
            var lowerKeyword = current.Keywords.ToLower();
            densities = densities.Where(x => (x.SourceLocationIdentifier != null && x.SourceLocationIdentifier.ToLower().Contains(lowerKeyword)) ||
                                             (x.SourceLocationName != null && x.SourceLocationName.ToLower().Contains(lowerKeyword))
                                          || (x.FacilityServiceName != null && x.FacilityServiceName.Any() &&
                                              x.FacilityServiceName.Any(facilityServiceName =>
                                                                            facilityServiceName.ToLower().Contains(lowerKeyword)))).ToList();
        }

        var myList = densities.ToList();
        var morePages = current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault() < myList.Count;
        var results = new SearchResultsModel<PreSetDensityConversionParams, SearchCriteriaModel>
        {
            Results = myList
                     .Skip(current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault())
                     .Take(current.PageSize.GetValueOrDefault()),
            Info = new()
            {
                TotalRecords = myList.Count,
                NextPageCriteria = morePages ? new SearchCriteriaModel { CurrentPage = current.CurrentPage + 1 } : null,
            },
        };

        _results = results;
        await Task.CompletedTask;
    }

    private async Task EditRow(PreSetDensityConversionParams densityRecord)
    {
        if (recordToUpdate != null && recordToUpdate.Id != Guid.Empty)
        {
            var updatedRecord = FacilityDefaultDensities.First(x => x.Id == recordToUpdate.Id);
            var index = FacilityDefaultDensities.IndexOf(updatedRecord);
            FacilityDefaultDensities[index] = recordToUpdate;
            recordToUpdate = null;
            await Refresh();
        }

        recordToUpdate = densityRecord.Clone();
        await _grid.EditRow(densityRecord);
    }

    private async Task SaveRow(PreSetDensityConversionParams densityRecord)
    {
        if (!densityRecord.IsEnabled)
        {
            await _grid.UpdateRow(densityRecord);
        }

        var isSourceLocationExist = densityRecord.SourceLocationId != null;
        var isFacilitySourceExist = densityRecord.FacilityServiceId != null && densityRecord.FacilityServiceId.Any();

        //Reject record if FacilityService is selected without SourceLocation
        if (densityRecord.SourceLocationId == null && densityRecord.FacilityServiceId != null && densityRecord.FacilityServiceId.Any())
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Invalid default densities record added.");
            return;
        }

        //Reject record if Start Date is not entered                                            
        if (densityRecord.StartDate == default)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Enter Valid Start Date.");
            return;
        }

        //Reject record if End Date is greater than Start Date                                            
        if (densityRecord.StartDate > densityRecord.EndDate)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Start Date should not be greater than End Date");
            return;
        }

        var matchingDefaultDensityRecords = FilterDefaultDensityRecords(FacilityDefaultDensities, densityRecord);

        //Update End date based on existing record
        var matchingDefaultDensityRecordsWithFutureStartDate = matchingDefaultDensityRecords
                                                              .Where(x => x.StartDate > densityRecord.StartDate).OrderBy(x => x.StartDate).ToList();

        if (matchingDefaultDensityRecordsWithFutureStartDate.Any())
        {
            var earliestStartDate = matchingDefaultDensityRecordsWithFutureStartDate.First().StartDate;
            densityRecord.EndDate = earliestStartDate.AddDays(-1);
        }

        matchingDefaultDensityRecords = FilterDefaultDensityRecords(matchingDefaultDensityRecords, densityRecord);

        //Reject record if overlapping SourceLocation pre-set densities
        if (matchingDefaultDensityRecords.Any() && isSourceLocationExist)
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       detail: "Pre-Set Density Configuration with same Source Location already exist.");

            return;
        }

        //Reject record if overlapping FacilityService pre-set densities
        if (matchingDefaultDensityRecords.Any() && isFacilitySourceExist)
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       detail: "Pre-Set Density Configuration with same Source Location & Facility Services combination already exist.");

            return;
        }

        //Reject record if overlapping Facility pre-set densities
        if (matchingDefaultDensityRecords.Any() && !isSourceLocationExist && !isFacilitySourceExist)
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       detail: "Duplicate Facility Pre-Set Density Configuration already exist.");

            return;
        }

        //Reject record based on TimePeriod of existing Default Density records

        if (FindOverlappingIntervals(matchingDefaultDensityRecords, densityRecord))
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Existing default densities with overlapping time period exists.");
            return;
        }

        densityRecord.IsValid = true;
        densityRecord.IsEnabled = true;
        recordToUpdate = null;
        await _grid.UpdateRow(densityRecord);
    }

    private List<PreSetDensityConversionParams> FilterDefaultDensityRecords(List<PreSetDensityConversionParams> defaultDensityRecords, PreSetDensityConversionParams densityRecord)
    {
        var endDate = densityRecord.EndDate ?? DateTimeOffset.MaxValue;

        return new(defaultDensityRecords.Where(defaultParams => defaultParams.Id != densityRecord.Id && defaultParams.IsEnabled)
                                        .Where(defaultParams => defaultParams.StartDate <= endDate && (defaultParams.EndDate is null ||
                                                                                                       defaultParams.EndDate >= densityRecord.StartDate))
                                        .Where(defaultParams => densityRecord.FacilityServiceId != null && densityRecord.FacilityServiceId.Any()
                                                                    ? defaultParams.FacilityServiceId.Intersect(densityRecord.FacilityServiceId).Any()
                                                                    : defaultParams.FacilityServiceId is null ||
                                                                      defaultParams.FacilityServiceId.Count == 0)
                                        .Where(defaultParams => densityRecord.SourceLocationId is null
                                                                    ? defaultParams.SourceLocationId is null
                                                                    : defaultParams.SourceLocationId == densityRecord.SourceLocationId).ToList());
    }

    private async Task CancelEdit(PreSetDensityConversionParams densityRecord)
    {
        if (recordToUpdate.Id == densityRecord.Id)
        {
            var index = FacilityDefaultDensities.IndexOf(densityRecord);
            FacilityDefaultDensities[index] = recordToUpdate;
            recordToUpdate = null;
        }

        _grid.CancelEditRow(densityRecord);
        await Refresh();
    }

    private void BeforeFacilityServiceLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(FacilityService.FacilityId)] = Facility.Id;
        criteria.PageSize = int.MaxValue;
    }

    private void HandleSourceLocationSelection(SourceLocation sourceLocation, PreSetDensityConversionParams densityRecord)
    {
        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).SourceLocationId = sourceLocation.Id;
        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).SourceLocationName = sourceLocation.SourceLocationName;
        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).SourceLocationIdentifier = sourceLocation.FormattedIdentifier;
        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).SourceLocationGeneratorName = sourceLocation.GeneratorName;
    }

    private void FetchSelectedFacilityServicesItemModel(Dictionary<Guid, FacilityService> selectedFacilityServices, PreSetDensityConversionParams densityRecord)
    {
        if (!selectedFacilityServices.Any())
        {
            FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).FacilityServiceId = new();
            FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).FacilityServiceName = new();
            return;
        }

        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).FacilityServiceId = new(selectedFacilityServices.Keys.ToList());
        _facilityServiceMapping[densityRecord.Id] = new List<Guid>(selectedFacilityServices.Keys.ToList());
        FacilityDefaultDensities.First(x => x.Id == densityRecord.Id).FacilityServiceName = new(selectedFacilityServices.Values.Select(x => x.FacilityServiceNumber).ToList());
    }

    private void DateRenderRestrictBackdate(DateRenderEventArgs args)
    {
        args.Disabled = args.Disabled || args.Date < DateTime.Today;
    }

    public async Task Refresh()
    {
        await _grid.ReloadGrid();
    }

    private async Task AddDefaultDensity()
    {
        SetupDefaultDensity();
        await Refresh();
    }

    private async Task DeleteRow(PreSetDensityConversionParams densityRecord)
    {
        FacilityDefaultDensities.Remove(FacilityDefaultDensities.First(x => x.Id == densityRecord.Id));
        await Refresh();
    }

    private void SetupDefaultDensity(bool isFirstRecord = false)
    {
        var id = Guid.NewGuid();
        FacilityDefaultDensities?.Add(new()
        {
            Id = id,
            IsEnabled = true,
            IsDeleteEnabled = true,
            IsValid = false,
            IsDefaultFacilityDefaultDensity = isFirstRecord,
            OilConversionFactor = 1,
            SolidsConversionFactor = 1,
            WaterConversionFactor = 1,
        });

        _facilityServiceMapping.TryAdd(id, new List<Guid>());
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPreSetFacilityDefaultDensities(new() { PageSize = 10 });
        await base.OnInitializedAsync();
    }

    private bool FindOverlappingIntervals(List<PreSetDensityConversionParams> defaultDensityRecords, PreSetDensityConversionParams currentRecord)
    {
        var overlappingInterval = new List<PreSetDensityConversionParams>();
        var densityRecords = new List<PreSetDensityConversionParams>();
        densityRecords.AddRange(defaultDensityRecords);
        densityRecords.Add(currentRecord);

        densityRecords = densityRecords.OrderBy(x => x.StartDate).ToList();
        for (var i = 0; i < densityRecords.Count - 1; i++)
        {
            var endDate = densityRecords[i].EndDate == null
                       || densityRecords[i].EndDate == DateTimeOffset.MinValue || densityRecords[i].EndDate == default
                              ? DateTimeOffset.MaxValue
                              : densityRecords[i].EndDate;

            if (endDate > densityRecords[i + 1].StartDate)
            {
                overlappingInterval.Add(densityRecords[i]);
                overlappingInterval.Add(densityRecords[i + 1]);
            }
        }

        return overlappingInterval.Any();
    }
}
