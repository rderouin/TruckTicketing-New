using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Extensions;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class NoteItem : BaseRazorComponent
{
    private string _currentUserName;

    private Note _internalNote = new();

    private bool _isDirty;

    private bool _isEditing;

    private bool _isUpdating;

    private RadzenTextArea _noteEditor;

    private bool _noteIsOwnedByCurrentUser = true;

    private string _replicatedValue;

    [Parameter]
    public bool ReadOnly { get; set; } = false;

    [Parameter]
    public Note Note { get; set; }

    [Parameter]
    public bool IsNew { get; set; }

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public EventCallback<Note> OnNoteUpdate { get; set; }

    [Parameter]
    public EventCallback<Note> OnEditCancel { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _isEditing = IsNew;
        _internalNote = Note?.Clone() ?? new();
        _replicatedValue = _internalNote.Comment;
        _currentUserName = Application?.User?.Principal?.Identity?.Name;
        _noteIsOwnedByCurrentUser = Application?.User?.UserId.ToString() == Note?.CreatedById;
    }

    private async Task OnEditCancelled()
    {
        if (!IsNew)
        {
            _isEditing = false;
        }

        _isDirty = false;
        if (OnEditCancel.HasDelegate)
        {
            await OnEditCancel.InvokeAsync(_internalNote);
        }
    }

    private async Task OnNoteEdit()
    {
        _isEditing = true;
        _internalNote = Note.Clone();
        await _noteEditor.Element.FocusAsync();
    }

    private async Task OnNoteUpdated()
    {
        _isUpdating = true;
        await OnNoteUpdate.InvokeAsync(_internalNote);
        _isUpdating = false;
    }

    private void OnInput(ChangeEventArgs args)
    {
        _isDirty = _internalNote.Comment != args?.Value?.ToString();
        _replicatedValue = args?.Value?.ToString();
    }
}
