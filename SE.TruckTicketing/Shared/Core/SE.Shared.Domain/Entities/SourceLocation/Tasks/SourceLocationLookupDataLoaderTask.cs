using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocation.Tasks;

public class SourceLocationLookupDataLoaderTask : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly IProvider<Guid, SourceLocationTypeEntity> _sourceLocationTypeProvider;

    public SourceLocationLookupDataLoaderTask(IProvider<Guid, SourceLocationTypeEntity> sourceLocationTypeProvider,
                                              IProvider<Guid, AccountEntity> accountProvider,
                                              IProvider<Guid, SourceLocationEntity> sourceLocationProvider)
    {
        _sourceLocationTypeProvider = sourceLocationTypeProvider;
        _accountProvider = accountProvider;
        _sourceLocationProvider = sourceLocationProvider;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        return await EnrichSourceLocationTypeInfo(context.Target) &&
               await EnrichGeneratorInfo(context.Target) &&
               await EnrichAssociatedSourceLocationInfo(context.Target) &&
               await UpdateAssociatedWellSourceLocationCodes(context.Target) &&
               await EnrichUsSourceLocationUniqueCheck(context.Target) &&
               await EnrichCaSourceLocationUniqueCheck(context.Target);
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        return Task.FromResult(true);
    }

    private async Task<bool> EnrichAssociatedSourceLocationInfo(SourceLocationEntity entity)
    {
        if (entity.AssociatedSourceLocationId == Guid.Empty)
        {
            return true;
        }

        var associatedSourceLocation = await _sourceLocationProvider.GetById(entity.AssociatedSourceLocationId);

        entity.AssociatedSourceLocationIdentifier = associatedSourceLocation?.Identifier;
        entity.AssociatedSourceLocationFormattedIdentifier =
            associatedSourceLocation?.CountryCode == CountryCode.US ? associatedSourceLocation?.SourceLocationName : associatedSourceLocation?.FormattedIdentifier;

        entity.AssociatedSourceLocationCode = associatedSourceLocation?.SourceLocationCode;

        return true;
    }

    private async Task<bool> UpdateAssociatedWellSourceLocationCodes(SourceLocationEntity entity)
    {
        if (entity.SourceLocationTypeCategory != SourceLocationTypeCategory.Surface)
        {
            return true;
        }

        var associatedWells = (await _sourceLocationProvider.Get(sourceLocation => sourceLocation.AssociatedSourceLocationId == entity.Id)).ToList();
        associatedWells.ForEach(well =>
                                {
                                    well.AssociatedSourceLocationIdentifier = entity.Identifier;
                                    well.AssociatedSourceLocationFormattedIdentifier = entity.Identifier;
                                    well.AssociatedSourceLocationCode = entity.SourceLocationCode;
                                    _sourceLocationProvider.Update(well, true);
                                });

        return true;
    }

    private async Task<bool> EnrichGeneratorInfo(SourceLocationEntity entity)
    {
        var generator = entity.GeneratorId != default ? await _accountProvider.GetById(entity.GeneratorId) : default;
        if (generator == default)
        {
            return true;
        }

        entity.GeneratorName = generator.Name;
        entity.GeneratorAccountNumber = generator.AccountNumber;
        entity.GeneratorProductionAccountContactName = generator.Contacts?.Find(contact => contact.Id == entity.GeneratorProductionAccountContactId)?.Name;

        return true;
    }

    private async Task<bool> EnrichSourceLocationTypeInfo(SourceLocationEntity entity)
    {
        var sourceLocationType = entity.SourceLocationTypeId != default ? await _sourceLocationTypeProvider.GetById(entity.SourceLocationTypeId) : default;
        if (sourceLocationType == default)
        {
            return true;
        }

        entity.SourceLocationTypeName = sourceLocationType.Name;
        entity.SourceLocationTypeCategory = sourceLocationType.Category;
        entity.CountryCode = sourceLocationType.CountryCode;
        entity.SourceLocationType = sourceLocationType;

        entity.FormattedIdentifierPattern = sourceLocationType.FormatMask?
                                                              .Replace("#", "[0-9]")
                                                              .Replace("@", "[A-z]")
                                                              .Replace("*", ".")
                                                              .Replace("/", "\\/");

        return true;
    }

    private async Task<bool> EnrichUsSourceLocationUniqueCheck(SourceLocationEntity entity)
    {
        if (entity.SourceLocationType is not { CountryCode: CountryCode.US })
        {
            return true;
        }

        var matches = await _sourceLocationProvider.Get(sourceLocation => !sourceLocation.IsDeleted &&
                                                                          sourceLocation.Id != entity.Id &&
                                                                          sourceLocation.SourceLocationName == entity.SourceLocationName &&
                                                                          sourceLocation.SourceLocationTypeId == entity.SourceLocationTypeId);

        entity.IsUnique = !matches.Any();

        return true;
    }

    private async Task<bool> EnrichCaSourceLocationUniqueCheck(SourceLocationEntity entity)
    {
        if (entity.SourceLocationType is not { CountryCode: CountryCode.CA })
        {
            return true;
        }

        var matches = await _sourceLocationProvider.Get(sourceLocation => !sourceLocation.IsDeleted &&
                                                                          sourceLocation.Id != entity.Id &&
                                                                          sourceLocation.Identifier == entity.Identifier &&
                                                                          sourceLocation.SourceLocationTypeId == entity.SourceLocationTypeId &&
                                                                          sourceLocation.CountryCode == CountryCode.CA);

        entity.IsUnique = !matches.Any();

        return true;
    }
}
