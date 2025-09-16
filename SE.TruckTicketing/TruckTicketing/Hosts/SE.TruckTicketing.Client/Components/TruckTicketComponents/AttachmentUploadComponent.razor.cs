using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class AttachmentUploadComponent
{
    private readonly string[] _acceptedExtensions = { "png", "jpg", "jpeg", "bmp", "pdf" };

    private readonly long _limit = 1024 * 1024;

    private bool _isUploading;

    private string _uploadErrorMessage;

    private string AcceptedExtensionsString => string.Join(", ", _acceptedExtensions.Select(x => $"*.{x}"));

    private TruckTicket Model { get; set; }

    private TruckTicketAttachment TtAttachment { get; } = new();

    [Parameter]
    public TruckTicket TruckTicketModel { get => Model; set => Model = value; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAttachment> AddAttachmentCallback { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task SaveButton_Clicked()
    {
        await AddAttachmentCallback.InvokeAsync(TtAttachment);
    }

    private string GeneratePath(string ticketId, string fileName)
    {
        return string.Concat(Model.Id.ToString(), "/", fileName);
    }

    private async Task InputFileChange(InputFileChangeEventArgs args)
    {
        TtAttachment.File = args.File.Name;
        TtAttachment.Path = GeneratePath(Model.Id.ToString(), args.File.Name);
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
            await UploadFile(args.File, TtAttachment);
        }
        finally
        {
            _isUploading = false;
        }

        //ttScannedAttachment.IsUploaded = true;
        // refresh the view
        //_existingAttachmentUri = await truckTicketService.GetAttachmentDownloadUrl(ttScannedAttachment); //UserProfileService.GetSignatureDownloadUrl();
        //StateHasChanged();
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

    private async Task UploadFile(IBrowserFile file, TruckTicketAttachment ttAttachment)
    {
        // get the upload URL with a SAS token
        var truckTicketAttachment = await TruckTicketService.GetAttachmentUploadUrl(Model.Key.Id, ttAttachment.File, ttAttachment.ContentType);

        // prep the stream
        var stream = file.OpenReadStream(_limit);
        var streamContent = new StreamContent(stream);
        streamContent.Headers.Add("x-ms-blob-type", "BlockBlob");
        streamContent.Headers.ContentType = new(file.ContentType);

        // upload the file
        await HttpClientFactory.CreateClient().PutAsync(truckTicketAttachment.Model.Uri, streamContent);
    }
}
