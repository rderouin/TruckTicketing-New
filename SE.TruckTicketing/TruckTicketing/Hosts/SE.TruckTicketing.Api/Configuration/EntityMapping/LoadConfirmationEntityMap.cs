using System.Linq;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.LoadConfirmation;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class LoadConfirmationEntityMap : EntityMapper<LoadConfirmationEntity>, IEntityMapper<LoadConfirmationEntity>
{
    private readonly IFacilityQueryFilterContextAccessor _queryFilterContextAccessor;

    public LoadConfirmationEntityMap(IFacilityQueryFilterContextAccessor queryFilterContextAccessor)
    {
        _queryFilterContextAccessor = queryFilterContextAccessor;
    }

    public override void Configure(EntityTypeBuilder<LoadConfirmationEntity> modelBinding)
    {
        modelBinding.Property(x => x.CustomerWatchListStatus).HasConversion<string>();
        modelBinding.Property(x => x.CustomerCreditStatus).HasConversion<string>();
        modelBinding.Property(e => e.Status).HasConversion<string>();
        modelBinding.Property(e => e.InvoiceStatus).HasConversion<string>();

        var queryFilterContext = _queryFilterContextAccessor.FacilityQueryFilterContext;
        if (queryFilterContext is not null && queryFilterContext.AllowedFacilityIds.Any())
        {
            modelBinding.HasQueryFilter(loadConfirmation => queryFilterContext.AllowedFacilityIds.Contains(loadConfirmation.FacilityId));
        }
    }
}
