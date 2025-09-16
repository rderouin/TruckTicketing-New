using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Email;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class EmailTemplateSenderDialog : BaseTruckTicketingComponent
{
    private dynamic _dialog;

    [Parameter]
    public string EventName { get; set; }

    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public EventCallback<EmailTemplateDeliveryRequestModel> OnRequestDelivering { get; set; }

    private EventCallback CloseDialogCallback => new(this, CloseDialog);

    public async Task Open()
    {
        var parameters = new Dictionary<string, object>
        {
            { nameof(EmailTemplateSenderForm.EventName), EventName },
            { nameof(EmailTemplateSenderForm.OnCancel), CloseDialogCallback },
            { nameof(EmailTemplateSenderForm.OnRequestDelivered), CloseDialogCallback },
            { nameof(EmailTemplateSenderForm.OnRequestDelivering), OnRequestDelivering },
        };

        _dialog = await DialogService.OpenAsync<EmailTemplateSenderForm>(Title ?? "Send Email", parameters,
                                                                         new()
                                                                         {
                                                                             Width = "500px",
                                                                         });
    }

    private void CloseDialog()
    {
        DialogService.Close(_dialog);
    }
}
