namespace SE.TokenService.Contracts.Api.Models.Accounts;

public class B2CApiConnectorContinuationResponse : B2CApiConnectorResponse
{
    public const string ActionDescriptor = "Continue";

    public B2CApiConnectorContinuationResponse()
    {
        Action = ActionDescriptor;
    }
}
