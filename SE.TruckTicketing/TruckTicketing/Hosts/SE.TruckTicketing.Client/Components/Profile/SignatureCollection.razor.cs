using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Client.Components.Profile;

public partial class SignatureCollection
{
    private readonly string[] _acceptedExtensions = { "png", "jpg", "jpeg", "bmp" };

    private string _existingSignatureUri;

    private bool _isUploading;

    private long _limit = 1024 * 1024;

    private string _uploadErrorMessage;

    private string AcceptedExtensionsString => string.Join(", ", _acceptedExtensions.Select(x => $"*.{x}"));

    [Inject]
    public IAppSettings AppSettings { get; set; }

    [Inject]
    public IUserProfileService UserProfileService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // init
        await WithLoadingScreen(async () =>
                                {
                                    _limit = Convert.ToInt64(AppSettings["Values:FileUploadSizeLimit"]);
                                    _existingSignatureUri = await UserProfileService.GetSignatureDownloadUrl();
                                });

        await base.OnInitializedAsync();
    }

    private async Task InputFileChange(InputFileChangeEventArgs args)
    {
        if (!IsAcceptedExtension(args.File.Name))
        {
            _uploadErrorMessage = $"Invalid file type, allowed extensions: {AcceptedExtensionsString}";
            return;
        }

        _uploadErrorMessage = null;

        try
        {
            _isUploading = true;

            // upload the file
            await UploadFile(args.File);
        }
        finally
        {
            _isUploading = false;
        }

        // refresh the view
        _existingSignatureUri = await UserProfileService.GetSignatureDownloadUrl();
        StateHasChanged();
    }

    private bool IsAcceptedExtension(string fileName)
    {
        var ext = fileName?.Split('.').LastOrDefault();
        if (!ext.HasText())
        {
            return false;
        }

        return _acceptedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private async Task UploadFile(IBrowserFile file)
    {
        // get the upload URL with a SAS token
        var uri = await UserProfileService.GetSignatureUploadUrl();

        // prep the stream
        var stream = file.OpenReadStream(_limit);
        var streamContent = new StreamContent(stream);
        streamContent.Headers.Add("x-ms-blob-type", "BlockBlob");
        streamContent.Headers.ContentType = new(file.ContentType);

        // upload the file
        await HttpClientFactory.CreateClient().PutAsync(uri, streamContent);
    }
}
