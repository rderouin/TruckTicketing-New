using System.Collections.Generic;

namespace SE.Shared.Domain.LegalEntity;

public class LegalEntityConfiguration
{
    public const string Section = "LegalEntity";

    public List<string> IgnorePrimaryContactRequiredByBusinessSteam { get; set; } = new();
}
