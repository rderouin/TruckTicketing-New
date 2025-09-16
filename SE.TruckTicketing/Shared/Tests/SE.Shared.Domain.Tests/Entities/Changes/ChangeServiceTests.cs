using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Newtonsoft.Json.Linq;

using SE.Shared.Domain.Entities.Changes;

using Trident.Domain;

namespace SE.Shared.Domain.Tests.Entities.Changes;

[TestClass]
public class ChangeServiceTests
{
    [TestMethod]
    public void ChangeService_GoThrough()
    {
        // arrange
        var obj = new
        {
            Property = "Simple Property",
            Object = new
            {
                AnotherProperty = "Another Property",
            },
            SimpleArray = new List<string>
            {
                "apples",
                "banana",
                "oranges",
            },
            ComplexArray = new List<object>
            {
                new
                {
                    AnotherObjectProperty = "Another Object Property",
                },
            },
            TridentPrimitiveCollection = new PrimitiveCollection<string>
            {
                List = new()
                {
                    "cherries",
                    "melons",
                },
            },
        };

        var jObject = JToken.FromObject(obj);
        var log = new List<JToken>();

        Action<JToken> action = t => log.Add(t);

        // act
        ChangeService.GoThrough(jObject, action);

        // assert
        log.Count.Should().Be(16);
    }

    [TestMethod]
    public void ChangeService_IsTridentPrimitiveCollection()
    {
        // arrange
        var j = JToken.FromObject(new PrimitiveCollection<string>
        {
            List = new()
            {
                "cherries",
                "melons",
            },
        });

        // act
        var result = ChangeService.IsTridentPrimitiveCollection(j);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void ChangeService_GoThrough_WithReplace()
    {
        // arrange
        var jObject = JToken.FromObject(new
        {
            Property = "Simple Property",
            Object = new
            {
                AnotherProperty = "Another Property",
            },
            SimpleArray = new List<string>
            {
                "apples",
                "banana",
                "oranges",
            },
            ComplexArray = new List<object>
            {
                new
                {
                    AnotherObjectProperty = "Another Object Property",
                },
            },
            TridentPrimitiveCollection = new PrimitiveCollection<string>
            {
                List = new()
                {
                    "cherries",
                    "melons",
                },
            },
        });

        // act
        ChangeService.GoThrough(jObject, ChangeService.ReplaceTridentPrimitiveCollection);

        // assert
        var collection = jObject["TridentPrimitiveCollection"]!;
        collection.Should().BeOfType(typeof(JArray));
        collection.Count().Should().Be(2);
        collection[0]!.Value<string>().Should().Be("cherries");
        collection[1]!.Value<string>().Should().Be("melons");
    }
}
