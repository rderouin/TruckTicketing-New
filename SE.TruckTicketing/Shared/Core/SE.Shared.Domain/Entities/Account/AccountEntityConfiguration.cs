namespace SE.Shared.Domain.Entities.Account;

public class AccountEntityConfiguration
{
    public const string Section = "AccountEntity";

    public bool BypassPrimaryContactPhoneNumberConstraint { get; set; }
}
