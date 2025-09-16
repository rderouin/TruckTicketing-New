using System.Collections.Generic;
using System.Linq;

using Trident.Contracts.Configuration;

// ReSharper disable CollectionNeverUpdated.Global - this is a configuration object
// ReSharper disable UnusedAutoPropertyAccessor.Global - this is a configuration object
// ReSharper disable ClassNeverInstantiated.Global - this is a configuration object
// ReSharper disable MemberCanBePrivate.Global - this is a configuration object

namespace SE.Shared.Common.Changes;

public class ChangeConfiguration
{
    /// <summary>
    ///     Members to include into tracking without array notation:
    ///     - BillOfLading
    ///     - BillingContact.Name
    ///     - Attachments.File
    ///     - Signatories.ContactName
    /// </summary>
    public HashSet<string> MembersToInclude { get; set; } = new();

    /// <summary>
    ///     A flag to track all fields available on the entity.
    /// </summary>
    public bool TrackAllFields { get; set; }

    /// <summary>
    ///     A map of all fields to display names. Key is the field name.
    /// </summary>
    public Dictionary<string, string> DisplayNames { get; set; } = new();

    /// <summary>
    ///     Time to live configuration for the entity.
    /// </summary>
    public double? TimeToLive { get; set; }

    /// <summary>
    ///     A field map for a customized Tag value.
    /// </summary>
    public Dictionary<string, string> TagMap { get; set; }

    /// <summary>
    ///     A map of formatters for each customized property. Key is the field name.
    /// </summary>
    public Dictionary<string, ChangeConfigurationFormatter> Formatters { get; set; } = new();

    public bool HasFieldsToTrack()
    {
        return TrackAllFields || MembersToInclude?.Any() == true;
    }

    private ChangeConfiguration EnsureInit()
    {
        // ensure collections are initialized after loading configs
        MembersToInclude ??= new();
        DisplayNames ??= new();
        Formatters ??= new();
        TagMap ??= new();

        return this;
    }

    public static ChangeConfiguration Load(IAppSettings appSettings, string entity)
    {
        return appSettings.GetSection<ChangeConfiguration>($"Change:{entity}").EnsureInit();
    }
}
