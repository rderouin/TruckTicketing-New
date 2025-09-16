using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentFileUploader : BaseTruckTicketingComponent, IAsyncDisposable
{
    private dynamic _dialogResult;

    private InputFile _inputFileControl;

    private IJSInProcessObjectReference _scriptsModule;

    private TridentFileUploaderViewModel _viewModel;

    private bool ShowUploaderForm { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public bool UploadOnFileSelect { get; set; }

    [Parameter]
    public bool UploadOnFileSubmit { get; set; } = false;

    [Parameter]
    public string DialogTitle { get; set; } = "Upload files";

    [Parameter]
    public string TriggerText { get; set; } = "Upload files";

    [Parameter]
    public string CompleteButtonText { get; set; } = "Complete Upload";

    [Parameter]
    public string Accept { get; set; }

    [Parameter]
    public Func<FileUploadContext, Task<string>> UploadUriProvider { get; set; }

    [Parameter]
    public long FileSizeLimit { get; set; } = 1024 * 1024 * 10;

    [Parameter]
    public EventCallback<IEnumerable<FileUploadContext>> OnUploadComplete { get; set; }

    [Parameter]
    public bool UseDialog { get; set; } = true;

    [Parameter]
    public RenderFragment<TridentFileUploader> Template { get; set; }

    [Parameter]
    public bool DisableUpload { get; set; } = false;

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_scriptsModule is not null)
        {
            await _scriptsModule.DisposeAsync();
        }
    }

    private async Task UploadFile(FileUploadContext context)
    {
        // Needs better exception handling
        context.IsUploading = true;
        context.Progress = 99.9;
        _viewModel.RaiseUploadStateChangeEvent();

        try
        {
            // get the upload URL with a SAS token
            var uri = await UploadUriProvider(context);

            // prep the stream
            var stream = context.File.OpenReadStream(FileSizeLimit);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("x-ms-blob-type", "BlockBlob");
            streamContent.Headers.ContentType = new(context.File.ContentType);

            // upload the file
            await HttpClientFactory.CreateClient().PutAsync(uri, streamContent);
            context.Progress = 100;
            context.UploadComplete = true;
            _viewModel.RaiseUploadStateChangeEvent();
        }
        catch (Exception)
        {
            context.UploadError = true;
        }
        finally
        {
            context.IsUploading = false;
            _viewModel.RaiseUploadStateChangeEvent();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scriptsModule = await JsRuntime.InvokeAsync<IJSInProcessObjectReference>("import",
                                                                                      "./Components/TridentFileUploader.razor.js");
        }
    }

    protected async Task LoadFiles(InputFileChangeEventArgs args)
    {
        var contexts = Multiple ? args.GetMultipleFiles().Select(file => new FileUploadContext(file)).ToList() : new() { new(args.File) };
        _viewModel = new()
        {
            UploadContexts = contexts,
            CompleteButtonText = CompleteButtonText,
            TriggerUploadOnRender = !UseDialog && UploadOnFileSelect,
            TriggerUploadOnSubmit = UploadOnFileSubmit,
        };

        if (UseDialog)
        {
            _dialogResult = await DialogService.OpenAsync<TridentFileUploaderContexts>(DialogTitle, new()
            {
                { nameof(TridentFileUploaderContexts.ViewModel), _viewModel },
                { nameof(TridentFileUploaderContexts.OnCancel), new EventCallback(this, CloseDialog) },
                { nameof(TridentFileUploaderContexts.OnUpload), new EventCallback<IEnumerable<FileUploadContext>>(this, HandleUpload) },
                { nameof(TridentFileUploaderContexts.OnUploadComplete), new EventCallback<IEnumerable<FileUploadContext>>(this, HandleUploadComplete) },
            }, new()
            {
                ShowClose = false,
                CloseDialogOnOverlayClick = false,
            });
        }
        else
        {
            ShowUploaderForm = true;
            StateHasChanged();
        }
    }

    public async Task OpenFileChooser()
    {
        if (_scriptsModule is not null)
        {
            await _scriptsModule.InvokeVoidAsync("openTridentFileChooser", _inputFileControl.Element);
        }
    }

    private void CloseDialog()
    {
        if (UseDialog)
        {
            DialogService.Close(_dialogResult);
        }
    }

    private async Task HandleUpload(IEnumerable<FileUploadContext> contexts)
    {
        _viewModel.UploadedStarted = true;
        _viewModel.IsUploading = true;

        try
        {
            foreach (var context in contexts)
            {
                await UploadFile(context);
            }
        }
        finally
        {
            _viewModel.IsUploading = false;
        }

        if (UploadOnFileSubmit)
        {
            await HandleUploadComplete(contexts);
        }
    }

    private async Task HandleUploadComplete(IEnumerable<FileUploadContext> contexts)
    {
        await OnUploadComplete.InvokeAsync(contexts);
        CloseDialog();
    }
}

public class FileUploadContext
{
    public FileUploadContext(IBrowserFile file)
    {
        File = file;
    }

    public IBrowserFile File { get; }

    public bool IsUploading { get; set; }

    public bool UploadComplete { get; set; }

    public double Progress { get; set; }

    public bool UploadError { get; set; }

    public string Key => $"{File.Name}{File.Size}";
}
