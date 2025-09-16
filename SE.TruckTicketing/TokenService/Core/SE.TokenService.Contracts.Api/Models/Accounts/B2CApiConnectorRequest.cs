using System;

using Trident.Contracts.Api;

namespace SE.TokenService.Contracts.Api.Models.Accounts;

public class B2CApiConnectorRequest : ApiModelBase<Guid>
{
    public string Email { get; set; }

    public string DisplayName { get; set; }

    public string Surname { get; set; }

    public string GivenName { get; set; }

    public string ObjectId { get; set; }

    public Identity[] Identities { get; set; } = Array.Empty<Identity>();
}

public class Identity
{
    public string SignInType { get; set; }

    public string Issuer { get; set; }

    public string IssuerAssignedId { get; set; }
}
