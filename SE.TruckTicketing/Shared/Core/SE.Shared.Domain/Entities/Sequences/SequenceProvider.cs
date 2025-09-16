using System;

using Trident.Business;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Sequences;

public class SequenceProvider : ProviderBase<Guid, SequenceEntity>
{
    public SequenceProvider(ISearchRepository<SequenceEntity> repository) : base(repository)
    {
    }
}
