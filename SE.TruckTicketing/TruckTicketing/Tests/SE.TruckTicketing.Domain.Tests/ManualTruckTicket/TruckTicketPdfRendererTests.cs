using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;
using SE.TruckTicketing.Domain.LocalReporting;

using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket;

[TestClass]
public class TruckTicketPdfRendererTests
{
    [TestMethod("Renderer should return empty byte array when an empty ticket collection is passed.")]
    public void Renderer_RenderStubs_EmptyCollection()
    {
        // arrange
        var scope = new DefaultScope();
        var tickets = new List<TruckTicketEntity>();

        // act
        var bytes = scope.InstanceUnderTest.RenderTruckTicketStubs(tickets);

        // assert
        bytes.Length.Should().Be(0);
    }

    [TestMethod("Renderer should render scale ticket stub when ticket number ends in LF")]
    public void Renderer_RenderStubs_ScaleTicket()
    {
        // arrange
        var scope = new DefaultScope();
        scope.ConfigureDefaultReportDefinitionResolver();
        var tickets = new List<TruckTicketEntity>
        {
            new()
            {
                TicketNumber = "SADLF-100000013-LF",
                CountryCode = CountryCode.CA,
            },
        };

        // act
        var bytes = scope.InstanceUnderTest.RenderTruckTicketStubs(tickets);

        // assert
        bytes.Length.Should().BeGreaterThan(0);
        scope.ReportDefinitionResolverMock.Verify(resolver => resolver.GetReportDefinition(It.IsAny<ScaleTicketJournalItem>()), Times.Once);
    }

    [TestMethod("Renderer should render work ticket stubs when ticket number does not end in LF")]
    public void Renderer_RenderStubs_WorkTicket()
    {
        // arrange
        var scope = new DefaultScope();
        scope.ConfigureDefaultReportDefinitionResolver();

        var tickets = new List<TruckTicketEntity>
        {
            new()
            {
                TicketNumber = "SAFST-100000013-WT",
                CountryCode = CountryCode.CA,
            },
        };

        // act
        var bytes = scope.InstanceUnderTest.RenderTruckTicketStubs(tickets);

        // assert
        bytes.Length.Should().BeGreaterThan(0);
        scope.ReportDefinitionResolverMock.Verify(resolver => resolver.GetReportDefinition(It.IsAny<WorkTicketJournalItem>()), Times.Once);
    }

    private class DefaultScope : TestScope<ITruckTicketPdfRenderer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new TruckTicketPdfRenderer(ReportDefinitionResolverMock.Object);
        }

        public Mock<IReportDefinitionResolver> ReportDefinitionResolverMock { get; } = new();

        public void ConfigureDefaultReportDefinitionResolver()
        {
            ReportDefinitionResolverMock.Setup(resolver => resolver.GetReportDefinition(It.IsAny<TicketJournalItem>()))
                                        .Returns(new MemoryStream(File.ReadAllBytes("ManualTruckTicket/HelloWorldSampleReport.rdl")));
        }
    }
}
