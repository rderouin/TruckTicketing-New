using System;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public class LoadConfirmationSingleRequest
{
    public CompositeKey<Guid> LoadConfirmationKey { get; set; }

    public LoadConfirmationAction Action { get; set; }

    public bool IsCustomeEmail { get; set; }

    public string AdditionalNotes { get; set; }

    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }
}
