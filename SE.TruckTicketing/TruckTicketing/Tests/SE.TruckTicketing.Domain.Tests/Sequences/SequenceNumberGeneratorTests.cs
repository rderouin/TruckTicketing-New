using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Infrastructure;
using Trident.Contracts;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.IoC;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Sequences;

[TestClass]
public class SequenceNumberGeneratorTests
{
    private const int SequenceGenerationSeed = 10000;

    private const int SequenceGenerationBlockSize = 20;

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequenceManager_GenerateSequenceNumbers_RecordsExist()
    {
        //Arrange
        var scope = new DefaultScope();
        var results = new List<string>();
        //Act
        await foreach (var sequenceGenerationResults in scope.InstanceUnderTest.GenerateSequenceNumbers(nameof(SequenceTypes.InvoiceProposal), "DVFST", 15))
        {
            results.Add(sequenceGenerationResults);
        }

        //Assert
        Assert.IsTrue(results.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequenceManager_GenerateSequenceNumbers_RecordCount()
    {
        //Arrange
        var scope = new DefaultScope();
        var results = new List<string>();
        //Act
        await foreach (var sequenceGenerationResults in scope.InstanceUnderTest.GenerateSequenceNumbers("Invoice Proposl", "DVFST", 15))
        {
            results.Add(sequenceGenerationResults);
        }

        //Assert
        Assert.AreEqual(15, results.Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequenceManager_GenerateSequenceNumbers_Get_ExistingRecord()
    {
        //Arrange
        var scope = new DefaultScope();
        var results = new List<string>();
        var sequenceResult = new SequenceEntity
        {
            Type = nameof(SequenceTypes.InvoiceProposal),
            Prefix = "DVFST",
            LastNumber = 10150,
        };

        IEnumerable<SequenceEntity> getResult = new List<SequenceEntity> { sequenceResult };
        scope.SequenceProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<SequenceEntity, bool>>>(),
                                                    null,
                                                    It.IsAny<IEnumerable<string>>(),
                                                    false,
                                                    false,
                                                    true)).ReturnsAsync(getResult);

        //Act
        await foreach (var sequenceGenerationResults in scope.InstanceUnderTest.GenerateSequenceNumbers(nameof(SequenceTypes.InvoiceProposal), "DVFST", 15))
        {
            results.Add(sequenceGenerationResults);
        }

        //Assert
        Assert.AreEqual("DVFST-10151-LF", results[0]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequenceManager_GenerateSequenceNumbers_AppSettingsSeed_NewRequest()
    {
        //Arrange
        var scope = new DefaultScope();
        var results = new List<string>();

        //Act
        await foreach (var sequenceGenerationResults in scope.InstanceUnderTest.GenerateSequenceNumbers(nameof(SequenceTypes.InvoiceProposal), "DVFST", 15))
        {
            results.Add(sequenceGenerationResults);
        }

        //Assert
        Assert.AreEqual("DVFST-10001-LF", results[0]);
    }

    private class DefaultScope : TestScope<ISequenceNumberGenerator>
    {
        public DefaultScope()
        {
            AppSettingsMock.Setup(x => x.GetSection<SequenceConfiguration>(It.IsAny<string>())).Returns(new SequenceConfiguration
            {
                Infix = "-",
                MaxRequestBlockSize = SequenceGenerationBlockSize,
                Seed = SequenceGenerationSeed,
                Suffix = "-LF",
            });

            LeaseBlobStorageMock.Setup(x => x.AcquireLeaseAndExecute(It.IsAny<Func<Task<SequenceEntity>>>(),
                                                                     It.IsAny<string>(),
                                                                     It.IsAny<string>(),
                                                                     It.IsAny<TimeSpan?>(),
                                                                     It.IsAny<CancellationToken>()))
                                .Returns((Func<Task<SequenceEntity>> task, string _, string _, TimeSpan? _, CancellationToken _) => task());

            ContextFactoryMock.Setup(x => x.Create<IContext>(It.IsAny<Type>(), false)).Returns(ContextMock.Object);

            InstanceUnderTest = new SequenceManager(LoggerMock.Object,
                                                    SequenceProviderMock.Object,
                                                    AppSettingsMock.Object,
                                                    LeaseBlobStorageMock.Object,
                                                    ContextFactoryMock.Object,
                                                    ServiceLocatorMock.Object,
                                                    workflowManager: SequenceWorkflowManagerMock.Object);

            ServiceLocatorMock.Setup(x => x.CreateChildLifetimeScope()).Returns(ServiceLocatorMock.Object);

            ServiceLocatorMock.Setup(x => x.Get<IManager<Guid, SequenceEntity>>()).Returns(SequenceManagerMock.Object);

            ServiceLocatorMock.Setup(x => x.Get<ILog>()).Returns(LoggerMock.Object);
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, SequenceEntity>> SequenceProviderMock { get; } = new();

        public Mock<ILeaseObjectBlobStorage> LeaseBlobStorageMock { get; } = new();

        public Mock<IAbstractContextFactory> ContextFactoryMock { get; } = new();

        public Mock<IAppSettings> AppSettingsMock { get; } = new();

        public Mock<IContext> ContextMock { get; } = new();

        public Mock<IValidationManager<SequenceEntity>> SequenceValidationManagerMock { get; } = new();

        public Mock<IWorkflowManager<SequenceEntity>> SequenceWorkflowManagerMock { get; } = new();

        public Mock<IIoCServiceLocator> ServiceLocatorMock { get; } = new();

        public Mock<IManager<Guid, SequenceEntity>> SequenceManagerMock { get; } = new();
    }
}
