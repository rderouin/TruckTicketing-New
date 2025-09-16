using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

public class InvoiceConfigurationPermutationsIndexerTask : WorkflowTaskBase<BusinessContext<InvoiceConfigurationEntity>>
{
    private readonly List<InvoiceConfigurationPermutationsIndexEntity> _invoiceConfigurationPermutationsIndexEntities = new();

    private readonly IProvider<Guid, InvoiceConfigurationPermutationsIndexEntity> _invoiceConfigurationPermutationsIndexProvider;

    public InvoiceConfigurationPermutationsIndexerTask(IProvider<Guid, InvoiceConfigurationPermutationsIndexEntity> invoiceConfigurationPermutationsIndexProvider)
    {
        _invoiceConfigurationPermutationsIndexProvider = invoiceConfigurationPermutationsIndexProvider;
    }

    public override int RunOrder => 40;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<InvoiceConfigurationEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        if (context.Original == null || context.Original.PermutationsHash != context.Target.PermutationsHash)
        {
            await AddIndices(context);
            await CleanupExistingIndices(context);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceConfigurationEntity> context)
    {
        return Task.FromResult(context.Target != null && context.Target.Permutations != null && context.Target.Permutations.Any());
    }

    private async Task CleanupExistingIndices(BusinessContext<InvoiceConfigurationEntity> context)
    {
        if (context.Original == null)
        {
            return;
        }

        var existingIndices = await GetExistingIndices(context);
        if (existingIndices.Any())
        {
            foreach (var index in existingIndices)
            {
                await _invoiceConfigurationPermutationsIndexProvider.Delete(index, true);
            }
        }

        await Task.CompletedTask;
    }

    private async Task AddIndices(BusinessContext<InvoiceConfigurationEntity> context)
    {
        foreach (var permutation in context.Target.Permutations)
        {
            var invoiceConfigurationPermutationsIndex = new InvoiceConfigurationPermutationsIndexEntity
            {
                Id = Guid.NewGuid(),
                InvoiceConfigurationId = context.Target.Id,
                CustomerId = context.Target.CustomerId,
                Name = context.Target.Name,
                Number = permutation.Number,
                SourceLocation = permutation.SourceLocation,
                ServiceType = permutation.ServiceType,
                WellClassification = permutation.WellClassification,
                Substance = permutation.Substance,
                Facility = permutation.Facility,
            };

            invoiceConfigurationPermutationsIndex.InitPartitionKey();
            _invoiceConfigurationPermutationsIndexEntities.Add(invoiceConfigurationPermutationsIndex);
        }

        if (_invoiceConfigurationPermutationsIndexEntities.Count > 0)
        {
            foreach (var index in _invoiceConfigurationPermutationsIndexEntities)
            {
                await _invoiceConfigurationPermutationsIndexProvider.Insert(index, true);
            }
        }
    }

    private async Task<List<InvoiceConfigurationPermutationsIndexEntity>> GetExistingIndices(BusinessContext<InvoiceConfigurationEntity> context)
    {
        var invoiceConfiguration = context.Original;
        var partitionKey = InvoiceConfigurationPermutationsIndexEntity.GetPartitionKey(invoiceConfiguration.CustomerId);
        return (await _invoiceConfigurationPermutationsIndexProvider.Get(index => index.InvoiceConfigurationId == invoiceConfiguration.Id, partitionKey))?.ToList() ?? new(); // PK - OK
    }
}
