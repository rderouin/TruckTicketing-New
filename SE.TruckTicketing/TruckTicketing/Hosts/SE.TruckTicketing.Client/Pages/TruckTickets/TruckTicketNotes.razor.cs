using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketNotes : BaseTruckTicketingComponent
{
    private Notes _notes;

    public TruckTicket Model => ViewModel.TruckTicket;

    private string ThreadId => $"TruckTicket|{Model.Id}";

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    public IServiceBase<Note, Guid> NotesService { get; set; }

    public override void Dispose()
    {
        ViewModel.Initialized -= StateChange;
    }

    protected override void OnInitialized()
    {
        ViewModel.Initialized += StateChange;
    }

    protected async Task StateChange()
    {
        await _notes.Reload();
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> HandleNoteUpdate(Note note)
    {
        var response = note.Id == default ? await NotesService.Create(note) : await NotesService.Update(note);
        return response.IsSuccessStatusCode;
    }

    private async Task<SearchResultsModel<Note, SearchCriteriaModel>> LoadNotes(SearchCriteriaModel criteria)
    {
        return await NotesService.Search(criteria);
    }
}
