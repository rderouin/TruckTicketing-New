using System;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Product;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class AdditionalServicesConfigurationAdditionalServiceEntityInitializer
{
    public static AdditionalServicesConfigurationAdditionalServiceEntityInitializer Instance = new();

    public AdditionalServicesConfigurationAdditionalServiceEntity InitializeNew(ProductEntity product)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Name = product.Name,
            Number = product.Number,
            Quantity = 0,
            UnitOfMeasure = product.UnitOfMeasure,
            PullQuantityFromTicket = false,
        };
    }
}
