namespace SE.TokenService.Contracts.Api.Models.Accounts;

public class B2CApiConnectorBlockingResponse : B2CApiConnectorResponse
{
    public const string ActionDescriptor = "ShowBlockPage";

    public B2CApiConnectorBlockingResponse(string userMessage)
    {
        Action = ActionDescriptor;
        UserMessage = userMessage;
    }

    public string UserMessage { get; protected set; }
}
