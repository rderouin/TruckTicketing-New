namespace SE.Shared.Domain.EmailTemplates.EmailProcessors.LoadConfirmation;

public class LoadConfirmationTamperedEmailModel
{
    public string LoadConfirmationNumber { get; set; }

    public string OriginalFrom { get; set; }

    public string OriginalSubject { get; set; }
}
