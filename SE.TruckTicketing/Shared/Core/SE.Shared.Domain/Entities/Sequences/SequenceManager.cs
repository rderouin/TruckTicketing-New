using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Extensions;
using SE.Shared.Domain.Infrastructure;

using Trident.Business;
using Trident.Contracts;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.IoC;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;
using SE.TridentContrib.Extensions.Azure.Functions;

namespace SE.Shared.Domain.Entities.Sequences;

public class SequenceManager : ManagerBase<Guid, SequenceEntity>, ISequenceNumberGenerator
{
    private readonly IAppSettings _appSettings;

    private readonly ILeaseObjectBlobStorage _blobStorage;

    private readonly IAbstractContextFactory _contextFactory;

    private readonly IIoCServiceLocator _serviceLocator;

    private readonly IProvider<Guid, SequenceEntity> _provider;

    public SequenceManager(ILog logger,
                           IProvider<Guid, SequenceEntity> provider,
                           IAppSettings appSettings,
                           ILeaseObjectBlobStorage blobStorage,
                           IAbstractContextFactory contextFactory,
                           IIoCServiceLocator serviceLocator,
                           IValidationManager<SequenceEntity> validationManager = null,
                           IWorkflowManager<SequenceEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _provider = provider;
        _appSettings = appSettings;
        _blobStorage = blobStorage;
        _contextFactory = contextFactory;
        _serviceLocator = serviceLocator;
    }

    public async IAsyncEnumerable<string> GenerateSequenceNumbers(string sequenceType, string prefix, int count, string infix = default, string suffix = default)
    {
        var sequenceConfiguration = _appSettings.GetSequenceConfiguration(sequenceType);

        async Task<SequenceEntity> GetLatestSequence()
        {
            var sequence = (await Get(s => s.Type == sequenceType && s.Prefix == prefix)).FirstOrDefault(new SequenceEntity
            {
                Type = sequenceType,
                Prefix = prefix,
                LastNumber = sequenceConfiguration.Seed,
            });

            var isNew = sequence.Id == Guid.Empty;
            var existing = isNew ? null : sequence.Clone();
            sequence.LastNumber += count;
            AssureUniqueId(sequence);

            // We need a separate dbContext here to allow us commit the database operation of acquiring a sequence number,
            // within the boundary of the distributed lease (mutex lock) acquired around this local function,
            // while making sure that we still maintain the UnitOfWork managed by the cached context reused within Trident's repositories            
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // save in a separate context
            var sequenceManager = scope.Get<IManager<Guid, SequenceEntity>>();

            // init app-insights logger
            var logger = scope.Get<ILog>();
            var functionContextAccessor = new FunctionContextAccessor();
            logger.SetCallContext(functionContextAccessor.FunctionContext);

            // save the new/modified sequence
            await sequenceManager.Save(sequence, false);

            return sequence;
        }

        var sequence = await _blobStorage.AcquireLeaseAndExecute(GetLatestSequence, $"seq-{prefix}-{sequenceType}.lck");

        var remainingNumbers = count;
        while (remainingNumbers > 0)
        {
            yield return $"{sequence.Prefix}{infix ?? sequenceConfiguration.Infix}{sequence.LastNumber - remainingNumbers + 1}{suffix ?? sequenceConfiguration.Suffix}";
            remainingNumbers--;
        }
    }
}
