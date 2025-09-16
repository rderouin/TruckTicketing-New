using System;

using Humanizer;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Extensions;

public static class PrimaryAccountAddressExtensions
{
    public static string GetPrimaryAccountAddress(this AccountAddress primaryAccountAddress)
    {
        return String.Join(", ", primaryAccountAddress.Street, primaryAccountAddress.City, primaryAccountAddress.ZipCode, primaryAccountAddress.Province.Humanize(),
                           primaryAccountAddress.Country.Humanize());
    }
}
