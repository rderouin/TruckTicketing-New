using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentFileUploaderContexts : BaseTruckTicketingComponent
{
    public string _files;

    [Parameter]
    public TridentFileUploaderViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<FileUploadContext>> OnUpload { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<FileUploadContext>> OnUploadComplete { get; set; }

    [Inject]
    private ILogger<TridentFileUploaderContexts> Logger { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ViewModel.UploadStateChange += (_, __) => StateHasChanged();
        }

        if (ViewModel.UploadContexts.Count == 0 || !ViewModel.TriggerUploadOnRender)
        {
            return;
        }

        var files = string.Join(",", ViewModel.UploadContexts.Select(context => context.Key).OrderBy(key => key));
        if (_files != files)
        {
            _files = files;
            await HandleUpload();
        }
    }

    protected async Task HandleDelete(FileUploadContext context)
    {
        ViewModel.UploadContexts.Remove(context);
        StateHasChanged();

        if (ViewModel.UploadContexts.Count == 0)
        {
            await OnCancel.InvokeAsync();
        }
    }

    protected async Task HandleUpload()
    {
        try
        {
            await OnUpload.InvokeAsync(ViewModel.UploadContexts);

            if (ViewModel.TriggerUploadOnRender)
            {
                await HandleUploadComplete();
            }
        }
        catch (Exception)
        {
            ViewModel.IsUploading = false;
            StateHasChanged();
            throw;
        }
    }

    protected async Task HandleUploadComplete()
    {
        await OnUploadComplete.InvokeAsync(ViewModel.UploadContexts);
    }
}

public class TridentFileUploaderViewModel
{
    public delegate void UploadStateChangeEventHandler(object sender, EventArgs e);

    public string CompleteButtonText { get; set; }

    public List<FileUploadContext> UploadContexts { get; set; }

    public bool UploadedStarted { get; set; }

    public bool IsUploading { get; set; }

    public int CompletedUploads => UploadContexts?.Where(context => context.UploadComplete)?.Count() ?? 0;

    public bool IsComplete => CompletedUploads == UploadContexts.Count;

    public bool TriggerUploadOnRender { get; set; }

    public bool TriggerUploadOnSubmit { get; set; } = false;

    public bool HasError => UploadContexts?.Any(context => context.UploadError) ?? false;

    public event UploadStateChangeEventHandler UploadStateChange;

    public void RaiseUploadStateChangeEvent()
    {
        UploadStateChange?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        UploadedStarted = false;

        foreach (var context in UploadContexts)
        {
            context.Progress = 0;
            context.UploadError = false;
            context.UploadComplete = false;
        }
    }
}
