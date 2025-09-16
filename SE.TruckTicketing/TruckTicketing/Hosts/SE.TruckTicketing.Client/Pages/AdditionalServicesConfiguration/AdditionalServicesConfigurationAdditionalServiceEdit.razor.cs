using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using Trident.Api.Search;
using Trident.Extensions;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;

public partial class AdditionalServicesConfigurationAdditionalServiceEdit : BaseTruckTicketingComponent
{
    private AdditionalServicesConfigurationAdditionalService AdditionalServiceModel { get; set; }

    [Parameter]
    public Contracts.Models.Operations.AdditionalServicesConfiguration Model { get; set; }

    [Parameter]
    public AdditionalServicesConfigurationAdditionalService AdditionalService { get; set; }

    [Parameter]
    public bool IsNewRecord { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationAdditionalService> AddAdditionalService { get; set; }

    [Parameter]
    public EventCallback<AdditionalServicesConfigurationAdditionalService> UpdateAdditionalService { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        AdditionalServiceModel = AdditionalService.Clone();
    }

    private async Task SaveButton_Clicked()
    {
        if (!IsNewRecord)
        {
            await UpdateAdditionalService.InvokeAsync(AdditionalServiceModel);
        }
        else
        {
            await AddAdditionalService.InvokeAsync(AdditionalServiceModel);
        }
    }

    private void BeforeLoadingProductTypes(SearchCriteriaModel searchCriteria)
    {
        searchCriteria.Filters[nameof(Product.LegalEntityId)] = Model.LegalEntityId;

        if (Model.FacilityType == FacilityType.Lf)
        {
            var allowedCategories = new[]
            {
                ProductCategories.AdditionalServices.Lf,
                ProductCategories.AdditionalServices.Liner,
                ProductCategories.AdditionalServices.AltUnitOfMeasureClass1,
                ProductCategories.AdditionalServices.AltUnitOfMeasureClass2,
            };

            searchCriteria.Filters[nameof(Product.Categories)] = allowedCategories.AsInclusionAxiomFilter(nameof(Product.Categories).AsPrimitiveCollectionFilterKey());
        }
        else
        {
            var allowedCategories = new[] { ProductCategories.AdditionalServices.Fst };

            searchCriteria.Filters[nameof(Product.Categories)] = allowedCategories.AsInclusionAxiomFilter(nameof(Product.Categories).AsPrimitiveCollectionFilterKey());
        }

        var allowedSites = AxiomFilterBuilder
                          .CreateFilter()
                          .StartGroup()
                          .AddAxiom(new()
                           {
                               Field = nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey(),
                               Value = "",
                               Operator = CompareOperators.eq,
                               Key = "AllowedSites1",
                           })
                          .Or()
                          .AddAxiom(new()
                           {
                               Field = nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey(),
                               Value = Model.SiteId,
                               Operator = CompareOperators.contains,
                               Key = "AllowedSites2",
                           })
                          .EndGroup()
                          .Build();

        searchCriteria.Filters[nameof(Product.AllowedSites).AsPrimitiveCollectionFilterKey()] = allowedSites;
    }

    private void OnProductTypeChange(Product product)
    {
        AdditionalServiceModel.ProductId = product.Id;
        AdditionalServiceModel.Name = product.Name;
        AdditionalServiceModel.Number = product.Number;
        AdditionalServiceModel.Quantity = default;
        AdditionalServiceModel.UnitOfMeasure = product.UnitOfMeasure;
    }
}
