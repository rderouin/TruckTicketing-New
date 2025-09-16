using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Product;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class ProductEntityMapping : EntityMapper<ProductEntity>, IEntityMapper<ProductEntity>
{
    public override void Configure(EntityTypeBuilder<ProductEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.Substances, ProductSubstance =>
                                                 {
                                                     ProductSubstance.WithOwner();
                                                 });
    }
}
