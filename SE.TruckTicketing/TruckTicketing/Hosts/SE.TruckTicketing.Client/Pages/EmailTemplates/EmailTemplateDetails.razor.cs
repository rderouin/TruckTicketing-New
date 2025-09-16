using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.EmailTemplates;

public partial class EmailTemplateDetails : BaseTruckTicketingComponent
{
    private readonly string EmailValidationPattern = @"^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$";

    private Facility[] _facilities = Array.Empty<Facility>();

    private RadzenDropDownDataGrid<IEnumerable<string>> _facilityDropDownDataGrid;

    private SearchResultsModel<Facility, SearchCriteriaModel> _facilityResults = new();

    private bool _isSaving;

    protected RadzenListBox<object> EmailTemplateEventFieldsListBox;

    private bool DisableSenderEmailTextBox => !ViewModel.EmailTemplate.UseCustomSenderEmail;

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    [Inject]
    private IServiceProxyBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private IServiceProxyBase<EmailTemplateEvent, Guid> EmailTemplateEventService { get; set; }

    private IEnumerable<EmailTemplateEventField> EmailTemplateEventFields { get; set; }

    private IEnumerable<EmailTemplateEventField> EmailTemplateEventFieldsFiltered { get; set; }

    private IEnumerable<EmailTemplateEventAttachment> EmailTemplateEventAttachments { get; set; }

    [Parameter]
    public EmailTemplateDetailsViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected void HandleEmailTemplateEventChange(EmailTemplateEvent @event)
    {
        ViewModel.EmailTemplate.EventName = @event?.Name;

        EmailTemplateEventFields = @event?.Fields.OrderBy(field => field.UiToken) ?? Enumerable.Empty<EmailTemplateEventField>();
        EmailTemplateEventAttachments = @event?.Attachments.OrderBy(attachment => attachment.Name) ?? Enumerable.Empty<EmailTemplateEventAttachment>();

        EmailTemplateEventFieldsListBox.Reset();
    }

    protected void LoadEmailTemplateFields(LoadDataArgs args)
    {
        EmailTemplateEventFieldsFiltered = EmailTemplateEventFields?.Where(field => field.UiToken.ToLower().Contains(args.Filter.ToLower())).ToArray() ?? Array.Empty<EmailTemplateEventField>();
    }

    protected async Task HandleSubmit()
    {
        _isSaving = true;
        await OnSubmit.InvokeAsync();
        _isSaving = false;
    }

    private void ShowSenderEmailTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Provide valid sender email.", options);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _facilityResults = await FacilityService.Search(new() { PageSize = 250 }) ?? _facilityResults;
            _facilities = _facilityResults.Results.ToArray();
            StateHasChanged();
        }
    }

    protected async Task HandleEventFieldCopy(EmailTemplateEventField field)
    {
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", field.UiToken);
    }

    protected void HandleFacilityDropDownHeaderCheckboxChange(bool args)
    {
        ViewModel.EmailTemplate.FacilitySiteIds =
            args
                ? _facilities.Select(facility => facility.SiteId).ToList()
                : null;
    }

    protected void LoadFacilities(LoadDataArgs args)
    {
        var query = args.Filter.HasText()
                        ? _facilityResults.Results?.Where(facility => facility.SiteId.Contains(args.Filter, StringComparison.OrdinalIgnoreCase) ||
                                                                      facility.Name.Contains(args.Filter, StringComparison.OrdinalIgnoreCase))
                        : _facilityResults.Results;

        _facilities = query?.Skip(args.Skip ?? 0)
                            .Take(args.Top ?? 10)
                            .OrderBy(facility => facility.Name)
                            .ToArray() ?? _facilities;
    }
}

public class EmailTemplateDetailsViewModel
{
    public EmailTemplateDetailsViewModel(EmailTemplate emailTemplate)
    {
        EmailTemplate = emailTemplate;
        IsNew = emailTemplate.Id == default;
    }

    public string SubmitButtonText => IsNew ? "Add" : "Save";

    public string SubmitButtonBusyText => IsNew ? "Adding" : "Saving";

    public string Title => IsNew ? "Add Email Template" : "Edit Email Template " + EmailTemplate.Name;

    public bool IsNew { get; }

    public EmailTemplate EmailTemplate { get; set; }

    public Response<EmailTemplate> Response { get; set; }

    public Guid CustomerId
    {
        get => EmailTemplate.AccountIds?.FirstOrDefault() ?? Guid.Empty;
        set => EmailTemplate.AccountIds = value == default ? new() : new() { value };
    }

    public IEnumerable<string> FacilitySiteIds
    {
        get => EmailTemplate.FacilitySiteIds;
        set => EmailTemplate.FacilitySiteIds = value?.ToList();
    }
}
