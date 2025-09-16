using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class ChangeCollectionOwnerDialog : BaseRazorComponent
{
    private RadzenTemplateForm<InvoiceNotesViewModel> _form;

    private bool ShowComments { get; set; }

    private bool ReasonCommentValidatorIsEnabled => Model.CollectionReason == InvoiceCollectionReason.Other;

    private bool IsSubmitVisible => !IsReadOnly;

    [Parameter]
    public InvoiceNotesViewModel Model { get; set; } = new(Array.Empty<Invoice>());

    [Parameter]
    public EventCallback<InvoiceNotesViewModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public bool IsReadOnly { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }    

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(Model);
    }
}
