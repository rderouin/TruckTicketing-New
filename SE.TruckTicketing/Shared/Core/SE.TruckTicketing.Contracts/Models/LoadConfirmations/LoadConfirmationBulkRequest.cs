using System;
using System.Collections.Generic;
using System.Linq;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public class LoadConfirmationBulkRequest
{
    public List<CompositeKey<Guid>> LoadConfirmationKeys { get; set; }

    public LoadConfirmationAction Action { get; set; }

    public string AdditionalNotes { get; set; }

    public bool IsCustomeEmail { get; set; }

    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }

    public List<LoadConfirmationSingleRequest> ToSingleRequests()
    {
        return LoadConfirmationKeys.Select(key => new LoadConfirmationSingleRequest
        {
            LoadConfirmationKey = key,
            Action = Action,
            AdditionalNotes = AdditionalNotes,
            IsCustomeEmail = IsCustomeEmail,
            To = To,
            Cc = Cc,
            Bcc = Bcc,
        }).ToList();
    }
}
