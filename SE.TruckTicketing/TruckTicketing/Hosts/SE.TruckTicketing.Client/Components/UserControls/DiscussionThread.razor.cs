using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Enums;
using Trident.Mapper;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class DiscussionThread : BaseRazorComponent
{
    private SearchResultsModel<Note, SearchCriteriaModel> _results = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<Note>(),
    };

    private string commentValue = "";

    private List<NoteViewModel> listOfComments;

    private string runningComment;

    private int totalCommentsCounts;

    private readonly Dictionary<Guid, string> updatedList = new();

    public DiscussionThread()
    {
        listOfComments = new();
    }

    [Parameter]
    public string ThreadId { get; set; }

    //services injection
    [Inject]
    private IDiscussionThreadService DiscussionThreadService { get; set; }

    [Inject]
    private IMapperRegistry Mapper { get; set; }

    //Events
    [Parameter]
    public EventCallback<Note> OnNewComment { get; set; }

    [Parameter]
    public EventCallback<string> OnNewCommentText { get; set; }

    private bool IsThisMyComment(string createdById)
    {
        if (Application?.User?.UserId.ToString() == createdById)
        {
            return true;
        }

        return false;
    }

    private string GetCurrentUser()
    {
        return Application?.User?.Principal.Identity.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await PerformNoteAsyncSearch();
        listOfComments = Mapper.Map<List<NoteViewModel>>((List<Note>)_results.Results);
    }

    public async void CreateComment()
    {
        if (!string.IsNullOrWhiteSpace(commentValue))
        {
            var noteViewModel = new NoteViewModel
            {
                Comment = commentValue,
                ThreadId = ThreadId,
            };

            var resultNote = await DiscussionThreadService.Create(Mapper.Map<Note>(noteViewModel));
            listOfComments.Insert(0, Mapper.Map<NoteViewModel>(resultNote.Model));
            commentValue = string.Empty;
            runningComment = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task PerformNoteAsyncSearch()
    {
        var criteria = new SearchCriteriaModel
        {
            PageSize = 10,
            CurrentPage = 0,
            Keywords = "",
            OrderBy = "CreatedAt",
            Filters = new()
            {
                { "EntityType", "Note" },
                { "ThreadId", ThreadId },
            },
            SortOrder = SortOrder.Desc,
        };

        _results = await DiscussionThreadService.Search(criteria) ?? _results;
        totalCommentsCounts = _results.Info.TotalRecords;
        await InvokeAsync(StateHasChanged);
    }

    private void EditThisCommentButtonClicked(Guid commentId, string comment)
    {
        if (!updatedList.ContainsKey(commentId))
        {
            updatedList.Add(commentId, listOfComments.Where(x => x.Id == commentId).Select(x => x.Comment).FirstOrDefault());
        }
        else
        {
            updatedList[commentId] = comment;
        }
    }

    private void CancelTheChange(Guid commentId)
    {
        if (updatedList.ContainsKey(commentId))
        {
            listOfComments.Where(x => x.Id == commentId).FirstOrDefault().Comment = updatedList[commentId];
            updatedList.Remove(commentId);
        }
    }

    private async void UpdateCommentButtonClicked(Guid commentId)
    {
        if (updatedList.ContainsKey(commentId))
        {
            var note = Mapper.Map<Note>(listOfComments.Where(x => x.Id == commentId).Select(x => x).FirstOrDefault());
            await DiscussionThreadService.Update(note);
            updatedList.Remove(commentId);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ShowMoreCommentsAsync()
    {
        var currentCount = listOfComments.Count;
        var criteria = new SearchCriteriaModel
        {
            PageSize = 10,
            CurrentPage = currentCount / 10,
            Keywords = "",
            OrderBy = "CreatedAt",
            Filters = new()
            {
                { "EntityType", "Note" },
                { "ThreadId", ThreadId },
            },
            SortOrder = SortOrder.Desc,
        };

        var nextResult = await DiscussionThreadService.Search(criteria);
        if (nextResult != null)
        {
            listOfComments.AddRange(Mapper.Map<List<NoteViewModel>>((List<Note>)nextResult.Results));
        }

        totalCommentsCounts = _results.Info.TotalRecords;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ShowLessCommentsAsync()
    {
        if (listOfComments.Count > 10)
        {
            listOfComments.RemoveRange(9, listOfComments.Count - 10);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task CommentAdded(KeyboardEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(runningComment))
        {
            await OnNewCommentText.InvokeAsync(runningComment);
        }
    }
}
