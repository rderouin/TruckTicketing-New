using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Blazor.Components;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages;

public partial class ServiceTypeDetails 
{
    private EditContext _editContext;

    private bool _isLoading;

    private bool _isSaving;

    private string _oilValidationUnitText = "%";

    private string _solidValidationUnitText = "%";

    private string _totalValidationUnitText = "%";

    private string _waterValidationUnitText = "%";

    [Inject]
    public IServiceTypeService ServiceTypeService { get; set; }

    [Inject]
    public IProductService ProductService { get; set; }

    [Inject]
    private IServiceProxyBase<LegalEntity, Guid> LegalEntityService { get; set; }

    [Parameter]
    public ServiceType ServiceType { get; set; } = new();

    [Parameter]
    public Guid? ServiceTypeId { get; set; }

    private Response<ServiceType> Response { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private string Breadcrumb { get; set; }

    private bool IsNew { get; set; }

    private string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    private bool SubmitButtonDisabled { get; set; } = true;

    private string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    private string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    private string Title { get; set; }

    private int TotalValueFormat => ServiceType.TotalThresholdType == SubstanceThresholdType.Percentage ? 1 : 2;

    private int WaterValueFormat => ServiceType.WaterThresholdType == SubstanceThresholdType.Percentage ? 1 : 2;

    private int OilValueFormat => ServiceType.OilThresholdType == SubstanceThresholdType.Percentage ? 1 : 2;

    private int SolidValueFormat => ServiceType.SolidThresholdType == SubstanceThresholdType.Percentage ? 1 : 2;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        if (ServiceTypeId != default)
        {
            await LoadServiceType(ServiceTypeId.Value);
        }
        else
        {
            await LoadServiceType();
        }

        _isLoading = false;
        await base.OnInitializedAsync();
    }

    private async Task LoadServiceType(Guid? id = null)
    {
        Breadcrumb = IsNew ? "New Service Type" : "Service Type";
        IsNew = ServiceTypeId == default;

        var serviceType = id is null ? new() : await ServiceTypeService.GetById(id.Value);
        ServiceType = serviceType;
        Title = IsNew ? "Creating Service Type" : $"Editing Service Type {serviceType.ServiceTypeId}";

        _totalValidationUnitText = serviceType.TotalFixedUnit == SubstanceThresholdFixedUnit.Undefined ? "%" : serviceType.TotalFixedUnit.GetDescription();
        _waterValidationUnitText = serviceType.WaterFixedUnit == SubstanceThresholdFixedUnit.Undefined ? "%" : serviceType.WaterFixedUnit.GetDescription();
        _oilValidationUnitText = serviceType.OilFixedUnit == SubstanceThresholdFixedUnit.Undefined ? "%" : serviceType.OilFixedUnit.GetDescription();
        _solidValidationUnitText = serviceType.SolidFixedUnit == SubstanceThresholdFixedUnit.Undefined ? "%" : serviceType.SolidFixedUnit.GetDescription();

        _editContext = new(serviceType);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }
    protected void HandleLegalEntityLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(LegalEntity.Name);
        criteria.Filters[nameof(LegalEntity.ShowAccountsInTruckTicketing)] = true;
    }
    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        SubmitButtonDisabled = !_editContext.IsModified();
    }

    private async Task SaveButton_Clicked()
    {
        _isSaving = true;
        var response = await ServiceTypeService.Update(ServiceType);

        _isSaving = false;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: IsNew ? "Service type added successfully" : "Service type edited successfully");
            NavigationManager.NavigateTo("/service-type-view");
        }
        else
        {
            Response = response;
        }
    }

    private async Task OnLegalEntityChange(LegalEntity selectedLegalEntity)
    {
        var legalEntity = await LegalEntityService.GetById(ServiceType.LegalEntityId);
        ServiceType.CountryCode = legalEntity.CountryCode;
        ServiceType.CountryCodeString = legalEntity.CountryCode.GetDescription();
        ServiceType.LegalEntityCode = selectedLegalEntity.Code;

        if (legalEntity.CountryCode == CountryCode.CA)
        {
            _totalValidationUnitText = ServiceType.TotalThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.M3 : "%";
            _waterValidationUnitText = ServiceType.WaterThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.M3 : "%";
            _oilValidationUnitText = ServiceType.OilThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.M3 : "%";
            _solidValidationUnitText = ServiceType.SolidThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.M3 : "%";
        }
        else
        {
            _totalValidationUnitText = ServiceType.TotalThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.Barrels : "%";
            _waterValidationUnitText = ServiceType.WaterThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.Barrels : "%";
            _oilValidationUnitText = ServiceType.OilThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.Barrels : "%";
            _solidValidationUnitText = ServiceType.SolidThresholdType != SubstanceThresholdType.Percentage ? SubstanceThresholdFixedUnits.Barrels : "%";
        }

        await Task.CompletedTask;
    }

    private void OnProductLoading(SearchCriteriaModel arg)
    {
        arg.SortOrder = SortOrder.Asc;
        arg.OrderBy = nameof(Product.Name);
        if (ServiceType.LegalEntityId != default)
        {
            arg.Filters[nameof(Product.LegalEntityId)] = ServiceType.LegalEntityId;
        }
    }

    private void OnThresholdChange(object value, string componentName)
    {
        var validationType = (SubstanceThresholdType)value;
        if (componentName == "TotalThresholdTypeTextBox")
        {
            _totalValidationUnitText = validationType == SubstanceThresholdType.Percentage ? "%" :
                                       ServiceType.CountryCode == CountryCode.US ? SubstanceThresholdFixedUnits.Barrels : SubstanceThresholdFixedUnits.M3;

            ServiceType.TotalFixedUnit = _totalValidationUnitText == SubstanceThresholdFixedUnits.M3 ? SubstanceThresholdFixedUnit.M3 :
                                         _totalValidationUnitText == SubstanceThresholdFixedUnits.Barrels ? SubstanceThresholdFixedUnit.Barrels : SubstanceThresholdFixedUnit.Undefined;

            ServiceType.TotalMinValue = default;
            ServiceType.TotalMaxValue = default;
        }
        else if (componentName == "WaterThresholdTypeTextBox")
        {
            _waterValidationUnitText = validationType == SubstanceThresholdType.Percentage ? "%" :
                                       ServiceType.CountryCode == CountryCode.US ? SubstanceThresholdFixedUnits.Barrels : SubstanceThresholdFixedUnits.M3;

            ServiceType.WaterFixedUnit = _waterValidationUnitText == SubstanceThresholdFixedUnits.M3 ? SubstanceThresholdFixedUnit.M3 :
                                         _waterValidationUnitText == SubstanceThresholdFixedUnits.Barrels ? SubstanceThresholdFixedUnit.Barrels : SubstanceThresholdFixedUnit.Undefined;

            ServiceType.WaterMinValue = default;
            ServiceType.WaterMaxValue = default;
        }
        else if (componentName == "OilThresholdTypeTextBox")
        {
            _oilValidationUnitText = validationType == SubstanceThresholdType.Percentage ? "%" :
                                     ServiceType.CountryCode == CountryCode.US ? SubstanceThresholdFixedUnits.Barrels : SubstanceThresholdFixedUnits.M3;

            ServiceType.OilFixedUnit = _oilValidationUnitText == SubstanceThresholdFixedUnits.M3 ? SubstanceThresholdFixedUnit.M3 :
                                       _oilValidationUnitText == SubstanceThresholdFixedUnits.Barrels ? SubstanceThresholdFixedUnit.Barrels : SubstanceThresholdFixedUnit.Undefined;

            ServiceType.OilMinValue = default;
            ServiceType.OilMaxValue = default;
        }
        else if (componentName == "SolidThresholdTypeTextBox")
        {
            _solidValidationUnitText = validationType == SubstanceThresholdType.Percentage ? "%" :
                                       ServiceType.CountryCode == CountryCode.US ? SubstanceThresholdFixedUnits.Barrels : SubstanceThresholdFixedUnits.M3;

            ServiceType.SolidFixedUnit = _solidValidationUnitText == SubstanceThresholdFixedUnits.M3 ? SubstanceThresholdFixedUnit.M3 :
                                         _solidValidationUnitText == SubstanceThresholdFixedUnits.Barrels ? SubstanceThresholdFixedUnit.Barrels : SubstanceThresholdFixedUnit.Undefined;

            ServiceType.SolidMinValue = default;
            ServiceType.SolidMaxValue = default;
        }
    }

    private void OnChange(Product typedValue, string componentName)
    {
        switch (componentName)
        {
            case "TotalItemDropDown":
                ServiceType.TotalItemName = typedValue.Name;
                ServiceType.TotalItemId = typedValue.Id;
                break;
            case "OilItemDropDown":
                ServiceType.OilItemName = typedValue.Name;
                ServiceType.OilItemId = typedValue.Id;
                break;
            case "WaterItemDropDown":
                ServiceType.WaterItemName = typedValue.Name;
                ServiceType.WaterItemId = typedValue.Id;
                break;
            case "SolidItemDropDown":
                ServiceType.SolidItemName = typedValue.Name;
                ServiceType.SolidItemId = typedValue.Id;
                break;
        }
    }
}
