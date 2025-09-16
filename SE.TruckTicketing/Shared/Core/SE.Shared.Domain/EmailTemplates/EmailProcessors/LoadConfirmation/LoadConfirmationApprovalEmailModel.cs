using System.IO;

using SE.Shared.Domain.Entities.LoadConfirmation;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors.LoadConfirmation;

public class LoadConfirmationApprovalEmailModel
{
    public LoadConfirmationEntity LoadConfirmation { get; set; }

    public string LoadConfirmationNumber { get; set; }

    public string LoadConfirmationHash { get; set; }

    public Stream LoadConfirmationStream { get; set; }

    public string ContentType { get; set; }

    public string Filename { get; set; }
}
