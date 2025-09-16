namespace SE.Shared.Common.Changes;

public class ChangeConfigurationFormatter
{
    /// <summary>
    ///     Type of the formatter to use: DOTNET, or CUSTOM
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    ///     The .NET type of the raw value parser for the value: System.DateTimeOffset, System.Boolean
    /// </summary>
    public string ValueType { get; set; }

    /// <summary>
    ///     The format string to use for the formatter:
    ///     DOTNET: yyyy-MM-dd
    ///     DOTNET: N2
    ///     CUSTOM: oneline, twolines
    /// </summary>
    public string Format { get; set; }
}
