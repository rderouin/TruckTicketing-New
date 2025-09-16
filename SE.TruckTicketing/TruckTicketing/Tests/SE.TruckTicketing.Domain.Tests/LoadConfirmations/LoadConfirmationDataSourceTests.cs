using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.LoadConfirmations;

[TestClass]
public class LoadConfirmationDataSourceTests
{
    [TestMethod]
    public void LoadConfirmationDataSource_CombineEdiForLoadConfirmationV1()
    {
        // arrange
        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "3",
                    },
                    new()
                    {
                        EDIFieldName = "Test-4",
                        EDIFieldValueContent = "4",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "5",
                    },
                    new()
                    {
                        EDIFieldName = "Test-4",
                        EDIFieldValueContent = "4",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "1",
                    },
                    new()
                    {
                        EDIFieldName = "Test-2",
                        EDIFieldValueContent = "2",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new(),
            },
            new()
            {
                EdiFieldValues = null,
            },
        };

        // act
        var ediString = LoadConfirmationDataSource.CombineEdiForLoadConfirmationV1(salesLines);

        // assert
        ediString.Count.Should().Be(2);
        ediString["Test-2"].Should().Be("2");
        ediString["Test-4"].Should().Be("4");
    }

    [TestMethod]
    public void LoadConfirmationDataSource_CombineEdiForLoadConfirmationV2()
    {
        // arrange
        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "3",
                    },
                    new()
                    {
                        EDIFieldName = "Test-4",
                        EDIFieldValueContent = "4",
                    },
                    new()
                    {
                        EDIFieldName = "Test-7",
                        EDIFieldValueContent = "7",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "5",
                    },
                    new()
                    {
                        EDIFieldName = "Test-4",
                        EDIFieldValueContent = "4",
                    },
                    new()
                    {
                        EDIFieldName = "Test-7",
                        EDIFieldValueContent = "7",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-1",
                        EDIFieldValueContent = "1",
                    },
                    new()
                    {
                        EDIFieldName = "Test-2",
                        EDIFieldValueContent = "2",
                    },
                    new()
                    {
                        EDIFieldName = "Test-7",
                        EDIFieldValueContent = "7",
                    },
                },
            },
            new()
            {
                EdiFieldValues = new()
                {
                    new()
                    {
                        EDIFieldName = "Test-7",
                        EDIFieldValueContent = "7",
                    },
                },
            },
        };

        // act
        var ediString = LoadConfirmationDataSource.CombineEdiForLoadConfirmationV2(salesLines);

        // assert
        ediString.Count.Should().Be(1);
        ediString["Test-7"].Should().Be("7");
    }

    [TestMethod]
    public void LoadConfirmationDataSource_FormatEdiFields()
    {
        // arrange
        var edi = new Dictionary<string, string>
        {
            ["AFE"] = "1243",
            ["ApproverCoding"] = "225466",
            ["CatalogNo"] = "225466",
            ["CostCentre"] = "225466",
            ["Major"] = "225466",
            ["Minor"] = "225466",
            ["PO"] = "225466",
        };

        // act
        var formatted = LoadConfirmationDataSource.FormatEdiFieldsV2(edi);

        // assert
        formatted.Should().Be($"AFE : 1243{Environment.NewLine}Approver Coding : 225466{Environment.NewLine}Catalog No : 225466{Environment.NewLine}Cost Centre : 225466{Environment.NewLine}Major : 225466{Environment.NewLine}Minor : 225466{Environment.NewLine}PO : 225466");
    }
}
