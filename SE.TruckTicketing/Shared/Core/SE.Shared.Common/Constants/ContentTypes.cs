using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace SE.Shared.Common.Constants;

public static class ContentTypes
{
    public const string JSON = "application/json";

    public const string XML = "application/xml";

    public const string Binary = "application/octet-stream";

    public static readonly HashSet<string> StringContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaTypeNames.Text.Plain,
        MediaTypeNames.Text.Html,
        MediaTypeNames.Text.Xml,
        MediaTypeNames.Text.RichText,
        MediaTypeNames.Application.Soap,
        MediaTypeNames.Application.Json,
        MediaTypeNames.Application.Xml,
    };
}
