using System;

namespace SE.TruckTicketing.Contracts;

public static partial class Containers
{
    public const string Billiing = nameof(Billiing);

    public const string BilliingHistory = nameof(BilliingHistory);

    public const string Integration = nameof(Integration);

    public const string IntegrationHistory = nameof(IntegrationHistory);

    public const string TruckTicketing = nameof(TruckTicketing);

    public const string Token = nameof(Token);

    /// <summary>
    ///     Provides a list of Partition Values
    /// </summary>
    public static class Partitions
    {
        public const string Accounts = nameof(Accounts);

        public const string Configuration = nameof(Configuration);

        public const string FacilityServices = nameof(FacilityServices);

        public const string Billing = nameof(Billing);
    }

    public static class Discriminators
    {
        public const String FacilityService = nameof(FacilityService);

        public const string Account = nameof(Account);

        public const string Permission = nameof(Permission);

        public const string Role = nameof(Role);

        public const string Facility = nameof(Facility);

        public const string Sequence = nameof(Sequence);

        public const string TruckTicket = nameof(TruckTicket);

        public const string ServiceType = nameof(ServiceType);

        public const string Product = nameof(Product);

        public const string Note = nameof(Note);

        public const string NavigationConfiguration = nameof(NavigationConfiguration);

        public const string EDIFieldDefinition = nameof(EDIFieldDefinition);

        public const string EDIFieldValue = nameof(EDIFieldValue);

        public const string EDIFieldLookup = nameof(EDIFieldLookup);

        public const string EDIValidationPatternLookup = nameof(EDIValidationPatternLookup);

        public const string BillingConfiguration = nameof(BillingConfiguration);

        public const string MatchPredicate = nameof(MatchPredicate);

        public const string SpartanProductParameter = nameof(SpartanProductParameter);

        public const string MaterialApproval = nameof(MaterialApproval);

        public const string LegalEntity = nameof(LegalEntity);

        public const string BusinessStream = nameof(BusinessStream);

        public const string TicketType = nameof(TicketType);

        public const string PricingRule = nameof(PricingRule);

        public const string TruckTicketVoidReason = nameof(TruckTicketVoidReason);

        public const string TruckTicketHoldReason = nameof(TruckTicketHoldReason);

        public const string TradeAgreementUpload = nameof(TradeAgreementUpload);
    }
}
