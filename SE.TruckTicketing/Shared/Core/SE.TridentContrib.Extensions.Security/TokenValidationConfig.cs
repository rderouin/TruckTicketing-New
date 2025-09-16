namespace SE.TridentContrib.Extensions.Security;

public class TokenValidationConfig
{
    public string IssuerSigningKey { get; set; }

    public string MetadataAddress { get; set; }

    public string ValidAudience { get; set; }

    public string ValidIssuer { get; set; }
}
