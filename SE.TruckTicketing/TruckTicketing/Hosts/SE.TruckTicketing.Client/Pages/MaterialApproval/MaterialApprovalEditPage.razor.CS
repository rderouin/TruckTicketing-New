using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;

using SE.TruckTicketing.Client.Components.MaterialApprovalComponents;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.MaterialApproval;

public partial class MaterialApprovalEditPage
{
    private EditContext _editContext;

    private bool _isDownloadingScaleTicket;

    private bool _isLoading;

    private bool _isSaving;

    private Response<Contracts.Models.Operations.MaterialApproval> _response;

    private MaterialApprovalViewModel _viewModel = new(new());

    
    private string ReturnUrl => "/material-approvals";
    private bool IsActive
    {
        get => _viewModel.MaterialApproval.IsActive;
        set => _viewModel.MaterialApproval.IsActive = value;
    }

    private int SelectedTabIndex { get; set; }

    [Parameter]
    public Guid? MaterialApprovalId { get; set; }

    [Parameter]
    public bool IsEditable { get; set; } = false;

    [Parameter]
    public string Operation { get; set; }

    [Parameter]
    public EventCallback<Contracts.Models.Operations.MaterialApproval> CreateOrUpdateMaterialApproval { get; set; }

    [Parameter]
    public Contracts.Models.Operations.MaterialApproval MaterialApproval { get; set; } = new();

    [Parameter]
    public Guid? LegalEntityId { get; set; } = default;

    [Parameter]
    public Guid? FacilityId { get; set; } = default;

    [Parameter]
    public Guid? SourceLocationId { get; set; } = default;

    [Parameter]
    public Guid? SourceLocationGeneratorId { get; set; } = default;

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private IMaterialApprovalService MaterialApprovalService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

  
    [Inject]
    private IServiceBase<Account, Guid> AccountsService { get; set; }

 
    private void HandleTabChange(FieldIdentifier field)
    {
        _editContext.NotifyFieldChanged(field);
    }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        if (MaterialApprovalId != default)
        {
            await LoadMaterialApproval(MaterialApprovalId.Value, Operation);
        }
        else
        {
            await LoadMaterialApproval();
            
        }
        
        _isLoading = false;
        await base.OnInitializedAsync();
    }

    private async Task LoadMaterialApproval(Guid? id = null, string operation = null)
    {
        var materialApproval = id is null ? new() : await MaterialApprovalService.GetById(id.Value);
        if (operation == "clone")
        {
            materialApproval.Id = default;
            materialApproval.AnalyticalExpiryDate = default;
            materialApproval.ApproximateDeliveryDate = null;
            materialApproval.AnalyticalExpiryDatePrevious = null;
            materialApproval.DisposalEstimateAmount = null;
            materialApproval.AccumulatedTonnage = 0;
        }

        if(IsEditable && id == null)
        {
            materialApproval =  MaterialApproval;
        }

        _viewModel = new(materialApproval);

        _editContext = new(materialApproval);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }

    private void HandleAccountContacts(List<AccountContactMap> accountContacts)
    {
        foreach (var contact in accountContacts.Where(contact => _viewModel.AccountContacts.All(x => x.Id != contact.Id)))
        {
            _viewModel.AccountContacts.Add(contact);
        }
    }

    private void RefreshAccountContacts(bool isRefreshContacts)
    {
        _viewModel.AccountContacts = new();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private async Task OnStatusChange(bool newStatus)
    {
        if (newStatus)
        {
            _viewModel.MaterialApproval.IsActive = newStatus;
            _viewModel.MaterialApproval.EndDate = default;
        }
        else
        {
            await DialogService.OpenAsync<MaterialApprovalEndDate>("Material Approval End Date",
                                                                   new()
                                                                   {
                                                                       { nameof(MaterialApprovalEndDate.Model), _viewModel.MaterialApproval },
                                                                   });

            if (_viewModel.MaterialApproval.EndDate != default)
            {
                _viewModel.MaterialApproval.IsActive = newStatus;
            }
        }

        await Task.CompletedTask;
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;
        var signatoryContacts = _viewModel.AccountContacts;
        var materialApprovalResponse = _viewModel.IsNew
                                           ? await MaterialApprovalService.Create(_viewModel.MaterialApproval)
                                           : await MaterialApprovalService.Update(_viewModel.MaterialApproval);

        _isSaving = false;

        if (materialApprovalResponse.IsSuccessStatusCode)
        {
            if (CreateOrUpdateMaterialApproval.HasDelegate && IsEditable)
            {
                DialogService.Close();
                await CreateOrUpdateMaterialApproval.InvokeAsync(materialApprovalResponse.Model);
            }
            else 
            { 
                NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
                _viewModel = new(materialApprovalResponse.Model, signatoryContacts);
            }
        }

        _response = materialApprovalResponse;
    }

    private async Task HandleDownloadMaterialApprovalScaleTicket()
    {
        _isDownloadingScaleTicket = true;
        var response = await MaterialApprovalService.DownloadMaterialApprovalScaleTicket(_viewModel.MaterialApproval.Id);
        if (response.IsSuccessStatusCode)
        {
            await FileDownloadService.DownloadFile($"{_viewModel.MaterialApproval.MaterialApprovalNumber}-Scale-Ticket-Stub.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                   MediaTypeNames.Application.Pdf);
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Something went wrong while trying to download a scale ticket for your material approval.");
        }

        _isDownloadingScaleTicket = false;
    }

    private void CloseButton_Click()
    {
        if (IsEditable)
        {
            DialogService.Close();
        }
        else
        {
            NavigationManager.NavigateTo(ReturnUrl);
        }
    }
}
