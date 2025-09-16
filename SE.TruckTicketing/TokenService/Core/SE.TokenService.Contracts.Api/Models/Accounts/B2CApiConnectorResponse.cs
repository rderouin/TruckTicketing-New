namespace SE.TokenService.Contracts.Api.Models.Accounts;

public class B2CApiConnectorResponse
{
    public B2CApiConnectorResponse()
    {
        Version = "1.0.0";
    }

    public string Version { get; set; }

    public string Action { get; set; }
}
