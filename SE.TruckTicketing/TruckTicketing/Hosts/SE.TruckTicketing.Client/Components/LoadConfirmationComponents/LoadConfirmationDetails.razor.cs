using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class LoadConfirmationDetails : BaseTruckTicketingComponent
{
    [Parameter]
    public LoadConfirmation Model { get; set; }

    private string ThreadId => $"LoadConfirmation|{Model.Id}";

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    public async Task OpenAsModal()
    {
        await DialogService.OpenAsync<LoadConfirmationDetails>($"Load Confirmation {Model.Number}",
                                                               new() { { nameof(Model), Model } },
                                                               new()
                                                               {
                                                                   Height = "80%",
                                                                   Width = "80%",
                                                               });
    }

    private async Task<SearchResultsModel<Note, SearchCriteriaModel>> LoadNotes(SearchCriteriaModel criteria)
    {
        return await NotesService.Search(criteria);
    }

    private async Task<bool> HandleNoteUpdate(Note note)
    {
        var response = note.Id == Guid.Empty ? await NotesService.Create(note) : await NotesService.Update(note);
        return response.IsSuccessStatusCode;
    }
}
