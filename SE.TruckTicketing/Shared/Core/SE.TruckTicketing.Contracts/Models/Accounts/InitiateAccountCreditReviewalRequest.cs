using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class InitiateAccountCreditReviewalRequest : GuidApiModelBase
{
    public Guid AccountId { get; set; }

    public string ToEmail { get; set; }

    public List<string> CcEmails { get; set; }
}
