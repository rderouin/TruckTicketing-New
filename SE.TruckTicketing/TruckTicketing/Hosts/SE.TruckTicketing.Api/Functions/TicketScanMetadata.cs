using System;
using System.Text.RegularExpressions;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Api.Functions;

public record TicketScanMetadata
{
    /// <summary>
    /// Matches the container and full path of the ticket scan blob url.
    /// e.g https://test.blob.core.windows.net/container1/KIFST123243-SP-EXT.pdf
    /// Supports optional matching for nested blob paths
    /// e.g https://test.blob.core.windows.net/container1/nested-folder/KIFST123243-SP-EXT.pdf
    /// </summary>
    private static readonly Regex TicketScanBlobPathPattern =
        new(@"/(?<Container>[a-zA-Z0-9-]{1,61}[a-zA-Z0-9])(?<Path>[a-zA-Z0-9-/]*/(?<File>(?<TicketNumber>[A-z0-9]{5}[0-9]+-(?<TicketType>[A-z]{2}))-(?<AttachmentSuffix>(?>int)|(?>ext)).+))$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string TicketNumber { get; init; }

    public TruckTicketType TicketType { get; init; }

    public string AttachmentSuffix { get; init; }

    public string File { get; init; }
    
    public string Path { get; init; }

    public string Container { get; init; }

    /// <summary>
    /// Creates a TicketScanMetadata instance using a blob URL.
    /// </summary>
    /// <param name="absoluteBlobUrl">The absolute blob URL to parse.</param>
    /// <returns>A TicketScanMetadata instance if the URL is valid, otherwise null.</returns>
    public static TicketScanMetadata FromBlobUrl(string absoluteBlobUrl)
    {
        var match = TicketScanBlobPathPattern.Match(absoluteBlobUrl ?? string.Empty);

        return match.Success
                   ? new TicketScanMetadata
                   {
                       File = match.Groups[nameof(File)].Value,
                       Path = match.Groups[nameof(Path)].Value.Trim('/'),
                       Container = match.Groups[nameof(Container)].Value,
                       TicketNumber = match.Groups[nameof(TicketNumber)].Value,
                       AttachmentSuffix = match.Groups[nameof(AttachmentSuffix)].Value,
                       TicketType = Enum.Parse<TruckTicketType>(match.Groups[nameof(TicketType)].Value.ToUpper()),
                   }
                   : null;
    }

    private AttachmentType GetAttachmentTypeOrDefault(AttachmentType fallbackAttachmentType)
    {
        return AttachmentSuffix.ToLower() switch
               {
                   "int" => AttachmentType.Internal,
                   "ext" => AttachmentType.External,
                   _ => fallbackAttachmentType,
               };
    }

    /// <summary>
    /// Retrieves the computed attachment type based on the given country code and parsed ticket type.
    /// </summary>
    /// <param name="countryCode">The country code used to determine attachment type.</param>
    /// <returns>The computed attachment type.</returns>
    public AttachmentType GetComputedAttachmentType(CountryCode countryCode)
    {
        return TicketType switch
               {
                   TruckTicketType.WT => countryCode is CountryCode.US ? GetAttachmentTypeOrDefault(AttachmentType.Internal) : AttachmentType.External,
                   _ => GetAttachmentTypeOrDefault(AttachmentType.External),
               };
    }
}
