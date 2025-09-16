using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class AccountContactModel
{
    public Guid AccountId { get; set; }

    [JsonProperty("IsPrimary")]
    public bool IsPrimaryAccountContact { get; set; }

    public bool IsActive { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    [JsonProperty("Phone")]
    public string PhoneNumber { get; set; }

    [JsonProperty("Title")]
    public string JobTitle { get; set; }

    public string Contact { get; set; }

    public string DataAreaId { get; set; }

    public List<AccountAddress> Addresses { get; set; }
}
