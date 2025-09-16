using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors.LoadConfirmation;

public class LoadConfirmationAdvancedEmailModel
{
    public bool IsCustomeEmail { get; set; }

    public string AdditionalNotes { get; set; }

    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }

    public static LoadConfirmationAdvancedEmailModel FromRequest(LoadConfirmationSingleRequest requestModel)
    {
        return new()
        {
            To = requestModel.To,
            Cc = requestModel.Cc,
            Bcc = requestModel.Bcc,
            IsCustomeEmail = requestModel.IsCustomeEmail,
            AdditionalNotes = requestModel.AdditionalNotes,
        };
    }
}
