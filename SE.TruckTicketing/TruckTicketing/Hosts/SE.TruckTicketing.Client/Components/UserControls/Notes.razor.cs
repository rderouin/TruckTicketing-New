using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

using Radzen;

using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class Notes : BaseRazorComponent
{
    private Note _newNote = new();

    private SearchResultsModel<Note, SearchCriteriaModel> _notesData = new();

    private string _threadId;

    private Virtualize<Note> _virtualizedNotes;

    [Parameter]
    public bool NewCommentIsReadOnly { get; set; } = false;

    [Parameter]
    public bool ShowNew { get; set; } = false;

    [Parameter]
    public Func<SearchCriteriaModel, Task<SearchResultsModel<Note, SearchCriteriaModel>>> OnDataLoad { get; set; }

    [Parameter]
    public Func<Note, Task<bool>> OnNoteUpdate { get; set; }

    [Inject]
    public IBlazorDownloadFileService DownloadFileService { get; set; }

    [Inject]
    public ICsvExportService CsvExportService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Parameter]
    public string ThreadId { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "Leave a comment...";

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_threadId != ThreadId && _virtualizedNotes is not null)
        {
            _threadId = ThreadId;
            await _virtualizedNotes.RefreshDataAsync();
        }
    }

    private async ValueTask<ItemsProviderResult<Note>> LoadNotes(ItemsProviderRequest request)
    {
        if (request.Count > 0)
        {
            var searchCriteria = new SearchCriteriaModel
            {
                PageSize = request.Count,
                CurrentPage = request.StartIndex / request.Count,
                OrderBy = nameof(Note.CreatedAt),
                SortOrder = SortOrder.Desc,
            };

            searchCriteria.AddFilter("DocumentType", $"NoteEntity|{ThreadId}");

            _notesData = OnDataLoad is not null ? await OnDataLoad(searchCriteria) : _notesData;
        }
        
        return new(_notesData?.Results ?? Array.Empty<Note>(), _notesData?.Info?.TotalRecords ?? 0);
    }

    private async Task OnNoteUpdated(Note note)
    {
        note.ThreadId = ThreadId;

        var success = await OnNoteUpdate.Invoke(note);
        if (success)
        {
            await _virtualizedNotes!.RefreshDataAsync();
            _newNote = new();
        }
        else
        {
            _newNote = note;
        }
    }

    private void OnNewNoteCancelled(Note _)
    {
        _newNote = new();
    }

    private async Task DownloadAllNotes()
    {
        var searchCriteria = new SearchCriteriaModel
        {
            PageSize = 100,
            CurrentPage = 0,
            OrderBy = nameof(Note.CreatedAt),
            SortOrder = SortOrder.Desc,
        };

        searchCriteria.AddFilter("DocumentType", $"NoteEntity|{ThreadId}");
        var continueSearch = false;
        IEnumerable<NotesColumns> noteResults = null;
        do
        {
            var notes = OnDataLoad is not null ? await OnDataLoad(searchCriteria) : new();
            if (notes.Info.TotalRecords > 0)
            {
                noteResults = noteResults ?? Enumerable.Empty<NotesColumns>().Concat(notes.Results.Select(x => GetNoteItemContent(x)));
                continueSearch = notes.Info.NextPageCriteria != null && notes.Results.ToList().Count > 0;
            }

            if (continueSearch)
            {
                // advance page
                searchCriteria.CurrentPage++;
            }
        } while (continueSearch);

        if (noteResults is not null)
        {
            await CsvExportService.Export($"notes-{ThreadId}.csv", noteResults);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Info, "No data available for download");
        }
    }

    private NotesColumns GetNoteItemContent(Note note)
    {
        return new()
        {
            Comment = note.Comment,
            CreatedBy = note.CreatedBy,
            CreatedAt = note.CreatedAt.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")
        };
    }

    private class NotesColumns
    {
        public string CreatedBy { get; set; }

        public string CreatedAt { get; set; }

        public string Comment { get; set; }
    }

    public async Task Reload()
    {
        await _virtualizedNotes.RefreshDataAsync();
    }
}
