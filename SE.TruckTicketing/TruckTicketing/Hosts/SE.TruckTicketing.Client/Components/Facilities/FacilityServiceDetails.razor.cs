using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Client.Contracts.Models;

namespace SE.TruckTicketing.Client.Components.Facilities;

public partial class FacilityServiceDetails : BaseTruckTicketingComponent
{
    private bool _isBusy;

    private IEnumerable<string> selectedValues = new List<string>();

    public FacilityServiceDetailsViewModel ViewModel { get; set; }

    [Parameter]
    public FacilityServiceDetailsViewModel Model { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<FacilityService> OnSubmit { get; set; }

    [Parameter]
    public Guid LegalEntityId { get; set; }

    [Inject]
    public IProductService ProductService { get; set; }

    private List<FacilitySubstanceViewModel> Substances { get; set; } = new();

    private async Task HandleCancel()
    {
        if (OnCancel.HasDelegate)
        {
            _isBusy = true;
            await OnCancel.InvokeAsync();
            _isBusy = false;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        ViewModel = Model.Clone();
        await base.OnInitializedAsync();
        await SelectSubstances();
    }

    private void OnLoadingServiceTypes(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(ServiceType.IsActive), true);
        criteria.AddFilter(nameof(ServiceType.LegalEntityId), LegalEntityId);
    }

    private async Task HandleServiceTypeSelection(ServiceType serviceType)
    {
        ViewModel.FacilityService.Description = serviceType.Description;
        ViewModel.FacilityService.TotalItemProductId = serviceType.TotalItemId;
        await SelectSubstances();
    }

    private async Task SelectSubstances()
    {
        if (ViewModel.FacilityService.TotalItemProductId != default)
        {
            var productId = ViewModel.FacilityService.TotalItemProductId ?? Guid.NewGuid();
            var productSubstances = await ProductService.GetById(productId);
            Substances = productSubstances.Substances.Select(x => new FacilitySubstanceViewModel
            {
                Id = x.Id.ToString(),
                SubstanceName = x.SubstanceName,
            }).ToList();

            var authSubstances = ViewModel.FacilityService.AuthorizedSubstances;
            if (authSubstances != null)
            {
                selectedValues = authSubstances.Select(x => x.ToString());
            }
        }
    }

    private async Task HandleSubmit()
    {
        if (OnSubmit.HasDelegate)
        {
            _isBusy = true;
            if (ViewModel.FacilityService.AuthorizedSubstances != null && !ViewModel.FacilityService.AuthorizedSubstances.Any())
            {
                ViewModel.FacilityService.AuthorizedSubstances = null;
            }

            ViewModel.FacilityService.FacilityServiceNumber = $"{ViewModel.FacilityService.SiteId}-{ViewModel.FacilityService.ServiceNumber}";
            await OnSubmit.InvokeAsync(ViewModel.FacilityService);
            _isBusy = false;
        }
    }

    private void SelectionChanged(IEnumerable<FacilitySubstanceViewModel> selectedSubstances)
    {
        var savedSubstances = selectedSubstances != null ? selectedSubstances.Select(x => Guid.Parse(x.Id)).ToList() : new();
        selectedValues = savedSubstances.Select(x => x.ToString());
        ViewModel.FacilityService.AuthorizedSubstances = savedSubstances;
    }
}

public class FacilityServiceDetailsViewModel
{
    public FacilityServiceDetailsViewModel(FacilityService facilityService)
    {
        FacilityService = facilityService;
        IsNew = FacilityService.Id == default;
    }

    public FacilityService FacilityService { get; set; }

    public Response<FacilityService> Response { get; set; }

    public bool IsNew { get; }
}

public class FacilitySubstanceViewModel : ModelBase<string>
{
    public override string Id { get; set; }

    public string SubstanceName { get; set; }
}
