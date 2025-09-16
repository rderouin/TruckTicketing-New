using System;

using Humanizer;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Extensions;

public static class ContactAddressExtensions
{
    public static string GetContactAddress(this ContactAddress contactAddress)
    {
        return String.Join(", ", contactAddress.Street, contactAddress.City, contactAddress.ZipCode, contactAddress.Province.Humanize(), contactAddress.Country.Humanize());
    }
}
