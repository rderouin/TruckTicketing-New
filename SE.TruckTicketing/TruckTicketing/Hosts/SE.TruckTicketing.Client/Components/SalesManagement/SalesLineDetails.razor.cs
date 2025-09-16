using System;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.SalesManagement
{
    public partial class SalesLineDetails
    {
        [Parameter]
        public SalesLine Model { get; set; }
        private string ThreadId => $"SalesLine|{Model.Id}";
        [Inject]
        public IServiceBase<Note, Guid> NotesService { get; set; }
        private async Task<SearchResultsModel<Note, SearchCriteriaModel>> OnDataLoad(SearchCriteriaModel criteria)
        {
            return await NotesService.Search(criteria);
        }
        private async Task<bool> HandleNoteUpdate(Note note)
        {
            var response = note.Id == default ? await NotesService.Create(note) : await NotesService.Update(note);
            return response.IsSuccessStatusCode;
        }
    }
}
