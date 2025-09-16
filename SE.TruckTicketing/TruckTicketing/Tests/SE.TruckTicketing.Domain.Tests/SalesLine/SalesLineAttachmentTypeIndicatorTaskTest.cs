using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineAttachmentTypeIndicatorTaskTest : TestScope<SalesLineAttachmentTypeIndicatorTask>
{
    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Update)]
    public async Task Task_ShouldRun_IfOperationIsInsertOrUpdate(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateSalesLineContext();
        context.Operation = operation;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Custom)]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Undefined)]
    public async Task Task_ShouldNotRun_IfOperationIsNotInsertOrUpdate(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateSalesLineContext();
        context.Operation = operation;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Update)]
    public async Task Task_ShouldNotRun_IfThereAreNoAttachments(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        SalesLineEntity salesLine = new()
        {
            Id = Guid.NewGuid(),
            SalesLineNumber = "Z123",
        };

        var context = scope.CreateSalesLineContext(salesLine);
        context.Operation = operation;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldUpdateAttachmentTypeIndicatorWithNeitherValue()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.SalesLine.Clone();

        var context = scope.CreateSalesLineContext(original);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.AttachmentIndicatorType.Should().Be(AttachmentIndicatorType.Neither);
    }

    [TestMethod]
    public async Task Task_ShouldUpdateAttachmentTypeIndicatorWithInternalValue()
    {
        // arrange
        var scope = new DefaultScope();
        SalesLineEntity salesLine = new()
        {
            Id = Guid.NewGuid(),
            SalesLineNumber = "Z123",
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = string.Empty,
                    File = "sample-int.pdf",
                    Path = "123/sample-int.pdf",
                },
            },
        };

        var context = scope.CreateSalesLineContext(salesLine);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.AttachmentIndicatorType.Should().Be(AttachmentIndicatorType.Internal);
    }

    [TestMethod]
    public async Task Task_ShouldUpdateAttachmentTypeIndicatorWithExternalValue()
    {
        // arrange
        var scope = new DefaultScope();
        SalesLineEntity salesLine = new()
        {
            Id = Guid.NewGuid(),
            SalesLineNumber = "Z123",
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = string.Empty,
                    File = "sample-ext.pdf",
                    Path = "123/sample-ext.pdf",
                },
            },
        };

        var context = scope.CreateSalesLineContext(salesLine);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.AttachmentIndicatorType.Should().Be(AttachmentIndicatorType.External);
    }

    [TestMethod]
    public async Task Task_ShouldUpdateAttachmentTypeIndicatorWithInternalExternalValue()
    {
        // arrange
        var scope = new DefaultScope();
        SalesLineEntity salesLine = new()
        {
            Id = Guid.NewGuid(),
            SalesLineNumber = "Z123",
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = string.Empty,
                    File = "sample-int.pdf",
                    Path = "123/sample-int.pdf",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = string.Empty,
                    File = "sample-ext.pdf",
                    Path = "123/sample-ext.pdf",
                },
            },
        };

        var context = scope.CreateSalesLineContext(salesLine);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.AttachmentIndicatorType.Should().Be(AttachmentIndicatorType.InternalExternal);
    }

    private class DefaultScope : TestScope<SalesLineAttachmentTypeIndicatorTask>
    {
        public readonly SalesLineEntity SalesLine =
            new()
            {
                Id = Guid.NewGuid(),
                SalesLineNumber = "Z123",
                Attachments = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Container = string.Empty,
                        File = "sample.pdf",
                        Path = "123/sample.pdf",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Container = string.Empty,
                        File = "test.pdf",
                        Path = "456/test.pdf",
                    },
                },
            };

        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<SalesLineEntity> CreateSalesLineContext(SalesLineEntity original = null)
        {
            if (original == null)
            {
                return new(SalesLine);
            }

            return new(original);
        }
    }
}
