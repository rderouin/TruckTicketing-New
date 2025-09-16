using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.InvoiceConfigurationComponents;

public partial class InvoiceInfoSelectSourceLocation
{
    private LegalEntity _customerLegalEntity;

    private Guid? _selectedSourceLocation;

    [Parameter]
    public EventCallback<SourceLocation> OnSelection { get; set; }

    [Parameter]
    public Guid? CustomerLegalEntityId { get; set; }

    [Inject]
    private IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    private async Task HandleSourceLocationSelection(SourceLocation sourceLocation)
    {
        await OnSelection.InvokeAsync(sourceLocation);
    }

    private async Task HandleSourceLocationLoading(SearchCriteriaModel criteria)
    {
        //Load Generators for Facility LegalEntity
        var countryCode = await LoadCustomerLegalEntity();
        if (countryCode == CountryCode.Undefined)
        {
            return;
        }

        //Apply filter on SourceLocation by generatorId before loading
        criteria.Filters[nameof(SourceLocation.CountryCode)] = countryCode.ToString();
    }

    private async Task<CountryCode> LoadCustomerLegalEntity()
    {
        if (CustomerLegalEntityId == null)
        {
            return CountryCode.Undefined;
        }

        //Load Customer LegalEntity if not already loaded
        if (_customerLegalEntity == null || _customerLegalEntity.Id == Guid.Empty)
        {
            _customerLegalEntity = await LegalEntityService.GetById(CustomerLegalEntityId.Value);
        }

        return _customerLegalEntity?.CountryCode ?? CountryCode.Undefined;
    }
}
