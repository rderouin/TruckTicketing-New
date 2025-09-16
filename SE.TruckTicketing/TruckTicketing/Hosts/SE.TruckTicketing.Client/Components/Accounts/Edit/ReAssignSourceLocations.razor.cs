using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Pages.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.Accounts.Edit;

public partial class ReAssignSourceLocations
{
    private RadzenTemplateForm<SourceLocationReassignment> ReferenceToForm;

    private bool _isSaveDisabled => !ReferenceToForm.EditContext.Validate();

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCreateNewGenerator { get; set; }

    [Parameter]
    public SourceLocationReassignment SourceLocationOwnerModel { get; set; } = new();

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync();
    }

    private async Task CreateNewGenerator()
    {
        await OnCreateNewGenerator.InvokeAsync();
    }

    private void GeneratorSelection(Account selectedGenerator)
    {
        SourceLocationOwnerModel.SelectedGenerator = selectedGenerator;
    }

    private void DateRenderRestrictFutureDate(DateRenderEventArgs args)
    {
        args.Disabled = args.Disabled || args.Date > DateTime.Today;
    }

    private void HandleOwnershipDateChange(DateTimeOffset? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        var startDate = value.Value;
        SourceLocationOwnerModel.OwnershipStartDate = new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 7, 0, 0, new(0)).ToAlbertaOffset();
    }
}
