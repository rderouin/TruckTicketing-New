using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Pages.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class SourceLocationDropDown<TValue> : TridentApiDropDownDataGrid<SourceLocation, TValue>
{
    protected string DisplayProperty => TextProperty.HasText() ? TextProperty : nameof(SourceLocation.Display);

    protected override Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(SourceLocation.IsActive)] = true;
        criteria.Filters[nameof(SourceLocation.IsDeleted)] = false;
        
        return base.BeforeDataLoad(criteria);
    }
    public async Task CreateOrUpdateSourceLocation(CountryCode CountryCode,Guid? SourceLocationId)
    {
        await DialogService.OpenAsync<SourceLocationDetailsPage>(SourceLocationId.HasValue ? $"Edit - Source Location" : $"New - Source Location", new()
        {
            { nameof(SourceLocationDetailsPage.LegalEntityCountryCode), CountryCode },
             { nameof(SourceLocationDetailsPage.IsEditable), true },
            { nameof(SourceLocationDetailsPage.Id), SourceLocationId },
            { "AddSourceLocation", new EventCallback<SourceLocation>(this, NewSourceLocationAdded) },
            { "EditSourceLocation", new EventCallback<SourceLocation>(this, UpdateSourceLocation) },
        }, new()
        {
            Width = "80%",
            Height = "90%",
        });
    }

    private async Task NewSourceLocationAdded(SourceLocation sourceLocation)
    {
        Value = (TValue)Convert.ChangeType(sourceLocation.Id, typeof(TValue));
        await Reload();
        await base.InvokeOnItemSelect(sourceLocation);
        StateHasChanged();
    }
    private async Task UpdateSourceLocation(SourceLocation sourceLocation)
    {
        Value = (TValue)Convert.ChangeType(sourceLocation.Id, typeof(TValue));
        await base.InvokeOnItemSelect(sourceLocation);
        StateHasChanged();
    }
}
