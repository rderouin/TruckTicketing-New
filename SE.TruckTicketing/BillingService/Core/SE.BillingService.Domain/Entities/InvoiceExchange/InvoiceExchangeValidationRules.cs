using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentValidation;

using SE.BillingService.Contracts.Api.Enums;
using SE.Shared.Domain;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

public class InvoiceExchangeValidationRules : FluentValidationRule<InvoiceExchangeEntity, TTErrorCodes>
{
    private readonly IProvider<Guid, InvoiceExchangeEntity> _provider;

    public InvoiceExchangeValidationRules(IProvider<Guid, InvoiceExchangeEntity> provider)
    {
        _provider = provider;
    }

    public override int RunOrder => 100;

    public override async Task Run(BusinessContext<InvoiceExchangeEntity> context, List<ValidationResult> errors)
    {
        // parent invoice exchange is used in validation to ensure proper inheritance
        var parentInvoiceExchangeId = context.Target.RootInvoiceExchangeId;
        if (parentInvoiceExchangeId.HasValue)
        {
            var parentInvoiceExchange = await _provider.GetById(parentInvoiceExchangeId.Value);
            context.ContextBag[nameof(InvoiceExchangeEntity.RootInvoiceExchangeId)] = parentInvoiceExchange;
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<InvoiceExchangeEntity> context, InlineValidator<InvoiceExchangeEntity> validator)
    {
        // it has to be named
        validator.RuleFor(e => e.PlatformCode)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_PlatformCodeRequired);

        // type must be specified
        validator.RuleFor(e => e.Type)
                 .NotEqual(default(InvoiceExchangeType))
                 .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_TypeRequired);

        // 'based on' should be specified if not global
        validator.RuleFor(e => e.RootInvoiceExchangeId)
                 .NotEmpty()
                 .When(e => e.Type != InvoiceExchangeType.Global)
                 .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_BasedOnInvoiceExchangeIdRequired);

        // validate against the base
        if (context.Target.RootInvoiceExchangeId.HasValue)
        {
            // fetch base if such exists
            context.ContextBag.TryGetValue(nameof(InvoiceExchangeEntity.RootInvoiceExchangeId), out var parentInvoiceExchangeObject);
            var parentInvoiceExchange = parentInvoiceExchangeObject as InvoiceExchangeEntity;

            // ensure link exists
            validator.RuleFor(e => e)
                     .Must(_ => parentInvoiceExchange != null)
                     .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_ParentRequiredForChild);

            // proper inheritance
            validator.RuleFor(e => e.SupportsFieldTickets)
                     .Must(e => e == parentInvoiceExchange?.SupportsFieldTickets)
                     .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_ParentSupportsFieldTicketsMustMatch);

            validator.RuleFor(e => e.InvoiceDeliveryConfiguration.MessageAdapterType)
                     .Must(e => e == parentInvoiceExchange?.InvoiceDeliveryConfiguration?.MessageAdapterType)
                     .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_ParentInvoiceAdapterMustMatch);

            validator.RuleFor(e => e.FieldTicketsDeliveryConfiguration.MessageAdapterType)
                     .Must(e => e == parentInvoiceExchange?.FieldTicketsDeliveryConfiguration?.MessageAdapterType)
                     .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_ParentFieldTicketAdapterMustMatch);
        }

        // configure rules based on the type
        switch (context.Target.Type)
        {
            case InvoiceExchangeType.Global:
                validator.RuleFor(e => e.BusinessStreamId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_BusinessStreamMustBeEmpty);

                validator.RuleFor(e => e.LegalEntityId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_LegalEntityMustBeEmpty);

                validator.RuleFor(e => e.BillingAccountId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_AccountMustBeEmpty);

                break;

            case InvoiceExchangeType.BusinessStream:
                validator.RuleFor(e => e.BusinessStreamId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_BusinessStreamMustNotBeEmpty);

                validator.RuleFor(e => e.LegalEntityId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_LegalEntityMustBeEmpty);

                validator.RuleFor(e => e.BillingAccountId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_AccountMustBeEmpty);

                break;

            case InvoiceExchangeType.LegalEntity:
                validator.RuleFor(e => e.BusinessStreamId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_BusinessStreamMustNotBeEmpty);

                validator.RuleFor(e => e.LegalEntityId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_LegalEntityMustNotBeEmpty);

                validator.RuleFor(e => e.BillingAccountId)
                         .Empty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_AccountMustBeEmpty);

                break;

            case InvoiceExchangeType.Customer:
                validator.RuleFor(e => e.BusinessStreamId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_BusinessStreamMustNotBeEmpty);

                validator.RuleFor(e => e.LegalEntityId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_LegalEntityMustNotBeEmpty);

                validator.RuleFor(e => e.BillingAccountId)
                         .NotEmpty()
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_AccountMustNotBeEmpty);

                break;
        }

        // one level below
        if (context.Target.Type == InvoiceExchangeType.Global)
        {
            validator.RuleFor(e => e.InvoiceDeliveryConfiguration.MessageAdapterType)
                     .NotEqual(default(MessageAdapterType))
                     .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_MessageAdapterTypeNotEmpty_ForInvoices);

            if (context.Target.SupportsFieldTickets)
            {
                validator.RuleFor(e => e.FieldTicketsDeliveryConfiguration.MessageAdapterType)
                         .NotEqual(default(MessageAdapterType))
                         .WithTridentErrorCode(TTErrorCodes.InvoiceExchange_MessageAdapterTypeNotEmpty_ForFieldTickets);
            }
        }
    }
}
