using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Newtonsoft.Json;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.AdditionalServicesConfiguration;

public partial class AdditionalServicesConfigurationPage
{
    private EditContext _editContext = new(new());

    private bool _isSaving;

    private AdditionalServicesConfigurationViewModel _model = new(new());

    private Response<Contracts.Models.Operations.AdditionalServicesConfiguration> _response;

    [Parameter]
    public Guid? Id { get; set; }

    [Inject]
    private IAdditionalServicesConfigurationService AdditionalServicesConfigurationService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Id != default)
        {
            await LoadAdditionalServicesConfigurationAsync(Id);
        }
        else
        {
            await LoadAdditionalServicesConfigurationAsync();
        }

        await base.OnInitializedAsync();
    }

    private void OnCustomerDropdownChange(Account customer)
    {
        _model.AdditionalServicesConfiguration.CustomerId = customer.Id;
        _model.AdditionalServicesConfiguration.CustomerName = customer.Name;
    }

    private void OnFacilityChange(Facility facility)
    {
        _model.AdditionalServicesConfiguration.SiteId = facility.SiteId;
        _model.AdditionalServicesConfiguration.FacilityId = facility.Id;
        _model.AdditionalServicesConfiguration.LegalEntityId = facility.LegalEntityId;
        _model.AdditionalServicesConfiguration.FacilityName = facility.Name;
        _model.AdditionalServicesConfiguration.FacilityType = facility.Type;
    }

    private void OnFacilityLoad(Facility facility)
    {
        _model.AdditionalServicesConfiguration.LegalEntityId = facility.LegalEntityId;
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;

        if (Id != null)
        {
            _model.AdditionalServicesConfiguration.Id = (Guid)Id;
        }

        var response = _model.IsNew
                           ? await AdditionalServicesConfigurationService.Create(_model.AdditionalServicesConfiguration)
                           : await AdditionalServicesConfigurationService.Update(_model.AdditionalServicesConfiguration);

        _isSaving = false;

        if (response.IsSuccessStatusCode)
        {
            var additionalServicesConfiguration = JsonConvert.DeserializeObject<Contracts.Models.Operations.AdditionalServicesConfiguration>(response.ResponseContent);
            NotificationService.Notify(NotificationSeverity.Success, _model.SubmitSuccessNotificationMessage);
            if (additionalServicesConfiguration != null)
            {
                _model.IsNew = false;
                NavigationManager.NavigateTo($"/additional-services-configuration/edit/{additionalServicesConfiguration.Id}");
            }
        }

        _response = response;
    }

    private async Task LoadAdditionalServicesConfigurationAsync(Guid? id = null)
    {
        var result = id is null ? new() : await AdditionalServicesConfigurationService.GetById(id.Value);

        _model = new(result);

        _editContext = new(result);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _model.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private void MatchPredicateChange(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        var matchPredicateList = new List<AdditionalServicesConfigurationMatchPredicate>(_model.AdditionalServicesConfiguration.MatchCriteria);

        matchPredicateList.Where(x => x.Id == matchPredicate.Id).Select(c =>
                                                                        {
                                                                            c = matchPredicate;
                                                                            return c;
                                                                        }).ToList();

        _model.AdditionalServicesConfiguration.MatchCriteria = matchPredicateList;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.MatchCriteria)));
    }

    private void MatchPredicateAdd(AdditionalServicesConfigurationMatchPredicate newMatchCriteria)
    {
        var matchCriteria = new List<AdditionalServicesConfigurationMatchPredicate>(_model.AdditionalServicesConfiguration.MatchCriteria)
        {
            newMatchCriteria,
        };

        _model.AdditionalServicesConfiguration.MatchCriteria = matchCriteria;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.MatchCriteria)));
    }

    private void AdditionalServiceChange(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        var additionalServices = new List<AdditionalServicesConfigurationAdditionalService>(_model.AdditionalServicesConfiguration.AdditionalServices);

        additionalServices.Where(x => x.Id == additionalService.Id).Select(c =>
                                                                           {
                                                                               c.Name = additionalService.Name;
                                                                               c.Quantity = additionalService.Quantity;
                                                                               c.PullQuantityFromTicket = additionalService.PullQuantityFromTicket;
                                                                               c.Number = additionalService.Number;
                                                                               c.ProductId = additionalService.ProductId;
                                                                               return c;
                                                                           }).ToList();

        _model.AdditionalServicesConfiguration.AdditionalServices = additionalServices;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.AdditionalServices)));
    }

    private void AdditionalServiceAdd(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        var additionalServices = new List<AdditionalServicesConfigurationAdditionalService>(_model.AdditionalServicesConfiguration.AdditionalServices)
        {
            additionalService,
        };

        _model.AdditionalServicesConfiguration.AdditionalServices = additionalServices;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.AdditionalServices)));
    }

    private void UpdateAdditionalServiceDeleted(AdditionalServicesConfigurationAdditionalService additionalService)
    {
        var existingAdditionalServices = new List<AdditionalServicesConfigurationAdditionalService>(_model.AdditionalServicesConfiguration.AdditionalServices);
        existingAdditionalServices.Remove(additionalService);
        _model.AdditionalServicesConfiguration.AdditionalServices = existingAdditionalServices;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.AdditionalServices)));
    }

    private void UpdateMatchPredicateDeleted(AdditionalServicesConfigurationMatchPredicate matchPredicate)
    {
        var existingMatchPredicates = new List<AdditionalServicesConfigurationMatchPredicate>(_model.AdditionalServicesConfiguration.MatchCriteria);
        existingMatchPredicates.Remove(matchPredicate);
        _model.AdditionalServicesConfiguration.MatchCriteria = existingMatchPredicates;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Contracts.Models.Operations.AdditionalServicesConfiguration.AdditionalServices)));
    }
}
