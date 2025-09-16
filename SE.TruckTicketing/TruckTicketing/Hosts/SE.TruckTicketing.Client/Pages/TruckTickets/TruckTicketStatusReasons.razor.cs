using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketStatusReasons : BaseTruckTicketingComponent
{
    [Inject]
    public IServiceBase<Note, Guid> NotesService { get; set; }

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
