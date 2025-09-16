using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

using SE.BillingService.Domain.InvoiceDelivery.Mapper;

using Trident.Testing.TestScopes;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Mapper;

[TestClass]
public class JsonFiddlerTests
{
    [DataTestMethod]
    [DataRow("test", JTokenType.String, typeof(string), DisplayName = "JsonFiddler can wrap into a string.")]
    [DataRow(123, JTokenType.Integer, typeof(long), DisplayName = "JsonFiddler can wrap into an integer.")]
    [DataRow(123.456, JTokenType.Float, typeof(double), DisplayName = "JsonFiddler can wrap into a double.")]
    public void JsonFiddler_WrapValue_AllTypes(object value, JTokenType expectedType, Type type)
    {
        // arrange

        // act
        var j = JsonFiddler.WrapValue(value);

        // assert
        j.Type.Should().Be(expectedType);
        j.Value.Should().BeOfType(type);
    }

    [TestMethod("JsonFiddler cannot wrap a unsupported value.")]
    public void JsonFiddler_WrapValue_UnsupportedType()
    {
        // arrange
        var value = DateOnly.MinValue;

        // act
        try
        {
            JsonFiddler.WrapValue(value);
        }
        catch (Exception x)
        {
            // assert
            x.Should().BeOfType<NotSupportedException>();
            return;
        }

        Assert.Fail("Must throw an exception");
    }

    [DataTestMethod]
    [DataRow("$.Property1", "prop1", DisplayName = "JsonFiddler should be able to read a simple property.")]
    [DataRow("$.Class1.Property2", "prop2", DisplayName = "JsonFiddler should be able to read a simple property of a child object.")]
    [DataRow("$.Array1[:1].Class2.Property3", "prop3.1", DisplayName = "JsonFiddler should be able to read a property from an array.")]
    public void JsonFiddler_ReadValue_Basics(string path, string expectedValue)
    {
        // arrange
        var scope = new DefaultScope();
        var subject = new
        {
            Property1 = "prop1",
            Class1 = new
            {
                Property2 = "prop2",
            },
            Array1 = new[]
            {
                new
                {
                    Class2 = new
                    {
                        Property3 = "prop3.1",
                    },
                },
                new
                {
                    Class2 = new
                    {
                        Property3 = "prop3.2",
                    },
                },
            },
        };

        var j = JObject.FromObject(subject);

        // act
        var actualValues = scope.InstanceUnderTest.ReadValue(j, path);

        // assert
        actualValues.Should().HaveCount(1);
        actualValues.Should().Contain(v => (string)v.Value == expectedValue);
    }

    [DataTestMethod]
    [DataRow("$.Array1[*].Class2.Property3", 3, "prop3.1", "prop3.3", DisplayName = "JsonFiddler should be able to read multiple values.")]
    public void JsonFiddler_ReadValue_Multiple(string path, int expectedCount, string expectedFirstValue, string expectedLastValue)
    {
        // arrange
        var scope = new DefaultScope();
        var subject = new
        {
            Property1 = "prop1",
            Class1 = new
            {
                Property2 = "prop2",
            },
            Array1 = new[]
            {
                new
                {
                    Class2 = new
                    {
                        Property3 = "prop3.1",
                    },
                },
                new
                {
                    Class2 = new
                    {
                        Property3 = "prop3.2",
                    },
                },
                new
                {
                    Class2 = new
                    {
                        Property3 = "prop3.3",
                    },
                },
            },
        };

        var j = JObject.FromObject(subject);

        // act
        var actualValues = scope.InstanceUnderTest.ReadValue(j, path);

        // assert
        actualValues.Should().HaveCount(expectedCount);
        actualValues.First().Value.Should().Be(expectedFirstValue);
        actualValues.Last().Value.Should().Be(expectedLastValue);
    }

    [DataTestMethod]
    [DataRow("*", DisplayName = "JsonFiddler should auto-correct values for a generic index.")]
    [DataRow("a-1", DisplayName = "JsonFiddler should auto-correct values for tagged arrays.")]
    public void JsonFiddler_WriteValue_AutoFix(string indexName)
    {
        // arrange
        var scope = new DefaultScope();
        var target = new JObject();
        var items = new[] { "Item-1", "Item-2", "Item-3" };
        var dynamicIndexNamesSet = new HashSet<string>
        {
            indexName
        };
        var outcomes = new
        {
            Initial = new List<bool>(),
            Items = new List<bool>(),
            Second = new List<bool>(),
        };

        var expected = JObject.FromObject(new
        {
            Rows = new[]
            {
                new
                {
                    InvoiceNumber = "INV000123",
                    ItemName = "Item-1",
                },
                new
                {
                    InvoiceNumber = "INV000123",
                    ItemName = "Item-2",
                },
                new
                {
                    InvoiceNumber = "INV000123",
                    ItemName = "Item-3",
                },
            },
        });

        // act
        outcomes.Initial.Add(scope.InstanceUnderTest.WriteValue(target, $"$.Rows[{indexName}].InvoiceNumber", "INV000123", new Dictionary<string, int?> { [indexName] = 0 }, dynamicIndexNamesSet));
        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            outcomes.Items.Add(scope.InstanceUnderTest.WriteValue(target, $"$.Rows[{indexName}].ItemName", item, new Dictionary<string, int?> { [indexName] = index }, dynamicIndexNamesSet));
        }

        var secondIndex = 0;
        do
        {
            outcomes.Second.Add(scope.InstanceUnderTest.WriteValue(target, $"$.Rows[{indexName}].InvoiceNumber", "INV000123", new Dictionary<string, int?> { [indexName] = secondIndex++ }, dynamicIndexNamesSet));
            if (secondIndex > 3)
            {
                break;
            }
        } while (outcomes.Second.Last());

        // assert
        outcomes.Initial.Count.Should().Be(1);
        outcomes.Initial.Should().AllBeEquivalentTo(false);
        outcomes.Items.Count.Should().Be(3);
        outcomes.Items.Should().AllBeEquivalentTo(false);
        outcomes.Second.Count.Should().Be(3);
        outcomes.Second.Take(outcomes.Second.Count - 1).Should().AllBeEquivalentTo(true);
        outcomes.Second.Last().Should().Be(false);
        target.Should().BeEquivalentTo(expected);
    }

    [DataTestMethod]
    [DataRow("single-property", DisplayName = "JsonFiddler should be able to set value of a single simple property.")]
    [DataRow("single-inner-property", DisplayName = "JsonFiddler should be able to set a value of a child class.")]
    [DataRow("single-array-value", DisplayName = "JsonFiddler should be able to add a value into an array.")]
    [DataRow("single-array-value-static-index", DisplayName = "JsonFiddler should be able to set a value in array with a static index.")]
    [DataRow("single-array-object", DisplayName = "JsonFiddler should be able to add an object into an array.")]
    [DataRow("single-general", DisplayName = "JsonFiddler should be able to write a single value.")]
    [DataRow("multiple-properties", DisplayName = "JsonFiddler should be able to write two properties into the same object.")]
    [DataRow("multiple-second-replaces", DisplayName = "JsonFiddler should be able to re-write a property.")]
    [DataRow("multiple-same-array", DisplayName = "JsonFiddler should be able to write a value into the same array.")]
    [DataRow("multiple-same-array-replacement", DisplayName = "JsonFiddler should be able to re-write the value of an array with the same index.")]
    [DataRow("multiple-general", DisplayName = "JsonFiddler should be able to write a value into the same array twice.")]
    [DataRow("double-array-nesting-unspecified", DisplayName = "JsonFiddler should be able to place elements into the first array without specifying.")]
    [DataRow("double-array-nesting-first", DisplayName = "JsonFiddler should be able to place elements into the first array.")]
    [DataRow("double-array-nesting-last", DisplayName = "JsonFiddler should be able to place elements into the second array.")]
    [DataRow("double-array-nesting-reverse", DisplayName = "JsonFiddler should be able to place elements into the tagged array.")]
    [DataRow("double-array-same-identifier", DisplayName = "JsonFiddler should be able to differentiate similar identifiers.")]
    [DataRow("double-array-correct-placement-breadth-first-ltr", DisplayName = "JsonFiddler should be able to place elements into the tagged array (breadth first).")]
    [DataRow("double-array-correct-placement-depth-first-rtl", DisplayName = "JsonFiddler should be able to place elements into the tagged array (depth first).")]
    [DataRow("double-array-correct-placement-combined", DisplayName = "JsonFiddler should be able to build a proper hierarchy.")]
    [DataRow("csv-compatible", DisplayName = "JsonFiddler should be able to have properties with random titles.")]
    public void JsonFiddler_WriteValue_AllCases(string useCaseName)
    {
        // arrange
        var scope = new DefaultScope();
        var target = new JObject();
        var useCases = scope.GetUseCases();
        var useCase = useCases[useCaseName];
        useCase.Print();

        // act
        foreach (var expression in useCase.Expressions)
        {
            scope.InstanceUnderTest.WriteValue(target, expression.Path, expression.Value, expression.PlacementHint, new HashSet<string>());
        }

        // assert
        Console.WriteLine(Environment.NewLine + "Resulting object:" + Environment.NewLine + target);
        JToken.DeepEquals(target, useCase.ExpectedObject).Should().BeTrue();
    }

    private class DefaultScope : TestScope<JsonFiddler>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public Dictionary<string, UseCase> GetUseCases()
        {
            return new()
            {
                ["single-property"] = new()
                {
                    Expressions = { new("$.prop", "value") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        prop = "value",
                    }),
                },
                ["single-inner-property"] = new()
                {
                    Expressions = { new("$.my_class.my_prop", "val123") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        my_class = new
                        {
                            my_prop = "val123",
                        },
                    }),
                },
                ["single-array-value"] = new()
                {
                    Expressions = { new("$.array1[*]", "arr-val1") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        array1 = new[] { "arr-val1" },
                    }),
                },
                ["single-array-value-static-index"] = new()
                {
                    Expressions = { new("$.array[5]", "val6") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        array = new[] { null, null, null, null, null, "val6" },
                    }),
                },
                ["single-array-object"] = new()
                {
                    Expressions = { new("$.array2[*].instance1", "a-val2") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        array2 = new[]
                        {
                            new
                            {
                                instance1 = "a-val2",
                            },
                        },
                    }),
                },
                ["single-general"] = new()
                {
                    Expressions = { new("$.class1.subclass1.arr1[*].info[0].val1", "test-value") },
                    ExpectedObject = JObject.FromObject(new
                    {
                        class1 = new
                        {
                            subclass1 = new
                            {
                                arr1 = new[]
                                {
                                    new
                                    {
                                        info = new[]
                                        {
                                            new
                                            {
                                                val1 = "test-value",
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    }),
                },
                ["multiple-properties"] = new()
                {
                    Expressions =
                    {
                        new("$.prop1", "val1"),
                        new("$.prop2", "val2"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        prop1 = "val1",
                        prop2 = "val2",
                    }),
                },
                ["multiple-second-replaces"] = new()
                {
                    Expressions =
                    {
                        new("$.prop", "original-value"),
                        new("$.prop", "replacement"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        prop = "replacement",
                    }),
                },
                ["multiple-same-array"] = new()
                {
                    Expressions =
                    {
                        new("$.array[0].prop1", "val1"),
                        new("$.array[0].prop2", "val2"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        array = new[]
                        {
                            new
                            {
                                prop1 = "val1",
                                prop2 = "val2",
                            },
                        },
                    }),
                },
                ["multiple-same-array-replacement"] = new()
                {
                    Expressions =
                    {
                        new("$.array[3].prop", "original5"),
                        new("$.array[3].prop", "replacement9"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        array = new[]
                        {
                            null,
                            null,
                            null,
                            new
                            {
                                prop = "replacement9",
                            },
                        },
                    }),
                },
                ["multiple-general"] = new()
                {
                    Expressions =
                    {
                        new("$.class1.subclass1.info[1].arr1[*].val1", "test-value1"),
                        new("$.class1.subclass1.info[1].arr1[2].val1", "test-value2"),
                        new("$.class1.subclass1.info[1].arr1[2].val2", "test-value3"),
                        new("$.class1.subclass1.info[1].arr1[2].a2[1]", "test-value4"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        class1 = new
                        {
                            subclass1 = new
                            {
                                info = new[]
                                {
                                    null,
                                    new
                                    {
                                        arr1 = new object[]
                                        {
                                            new
                                            {
                                                val1 = "test-value1",
                                            },
                                            null,
                                            new
                                            {
                                                val1 = "test-value2",
                                                val2 = "test-value3",
                                                a2 = new[] { null, "test-value4" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    }),
                },
                ["double-array-nesting-unspecified"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[first].arr2[second]", "val1"),
                        new("$.arr1[first].arr2[second]", "val2"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "val2" },
                            },
                        },
                    }),
                },
                ["double-array-nesting-first"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[first].arr2[second]", "val1", ("first", null)),
                        new("$.arr1[first].arr2[second]", "val2", ("first", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "val1" },
                            },
                            new
                            {
                                arr2 = new[] { "val2" },
                            },
                        },
                    }),
                },
                ["double-array-nesting-last"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[first].arr2[second]", "val1", ("second", null)),
                        new("$.arr1[first].arr2[second]", "val2", ("second", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "val1", "val2" },
                            },
                        },
                    }),
                },
                ["double-array-nesting-reverse"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[second].arr2[first]", "val1", ("first", null)),
                        new("$.arr1[second].arr2[first]", "val2", ("first", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "val1", "val2" },
                            },
                        },
                    }),
                },
                ["double-array-same-identifier"] = new()
                {
                    Expressions =
                    {
                        new("$.arr[arr-0].arr[arr-1]", "val-a2-1", ("arr-1", null)),
                        new("$.arr[arr-0].arr[arr-1]", "val-a2-2", ("arr-1", null)),
                        new("$.arr[arr-0].arr[arr-1]", "val-a1-3", ("arr-0", null)),
                        new("$.arr[arr-0].arr[arr-1]", "val-a1-4", ("arr-0", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr = new[]
                        {
                            new
                            {
                                arr = new[] { "val-a2-1", "val-a2-2" },
                            },
                            new
                            {
                                arr = new[] { "val-a1-3" },
                            },
                            new
                            {
                                arr = new[] { "val-a1-4" },
                            },
                        },
                    }),
                },
                ["double-array-correct-placement-breadth-first-ltr"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[first].arr2[second]", "1", ("first", null)),
                        new("$.arr1[first].arr2[second]", "2", ("first", null)),
                        new("$.arr1[first].arr2[second]", "3", ("first", null)),
                        new("$.arr1[first].arr2[second]", "4", ("second", null)),
                        new("$.arr1[first].arr2[second]", "5", ("second", null)),
                        new("$.arr1[first].arr2[second]", "6", ("second", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "1" },
                            },
                            new
                            {
                                arr2 = new[] { "2" },
                            },
                            new
                            {
                                arr2 = new[] { "3", "4", "5", "6" },
                            },
                        },
                    }),
                },
                ["double-array-correct-placement-depth-first-rtl"] = new()
                {
                    Expressions =
                    {
                        new("$.arr1[first].arr2[second]", "1", ("second", null)),
                        new("$.arr1[first].arr2[second]", "2", ("second", null)),
                        new("$.arr1[first].arr2[second]", "3", ("second", null)),
                        new("$.arr1[first].arr2[second]", "4", ("first", null)),
                        new("$.arr1[first].arr2[second]", "5", ("first", null)),
                        new("$.arr1[first].arr2[second]", "6", ("first", null)),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        arr1 = new[]
                        {
                            new
                            {
                                arr2 = new[] { "1", "2", "3" },
                            },
                            new
                            {
                                arr2 = new[] { "4" },
                            },
                            new
                            {
                                arr2 = new[] { "5" },
                            },
                            new
                            {
                                arr2 = new[] { "6" },
                            },
                        },
                    }),
                },
                ["double-array-correct-placement-combined"] = new()
                {
                    Expressions =
                    {
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "1", ("first", 0), ("second", 0)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "2", ("first", 0), ("second", 1)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "3", ("first", 1), ("second", 0)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "4", ("first", 1), ("second", 1)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "5", ("first", 2)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "6", ("first", 4)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "7", ("second", null)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "X", ("first", null)),
                        new("$.prop.another.stat[1].arr1[first].arr2[second].final", "8"),
                    },
                    ExpectedObject = JObject.FromObject(new
                    {
                        prop = new
                        {
                            another = new
                            {
                                stat = new[]
                                {
                                    null,
                                    new
                                    {
                                        arr1 = new[]
                                        {
                                            new
                                            {
                                                arr2 = new[] { new { final = "1" }, new { final = "2" } },
                                            },
                                            new
                                            {
                                                arr2 = new[] { new { final = "3" }, new { final = "4" } },
                                            },
                                            new
                                            {
                                                arr2 = new[] { new { final = "5" } },
                                            },
                                            null,
                                            new
                                            {
                                                arr2 = new[] { new { final = "6" }, new { final = "7" } },
                                            },
                                            new
                                            {
                                                arr2 = new[] { new { final = "8" } },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    }),
                },
                ["csv-compatible"] = new()
                {
                    Expressions =
                    {
                        new("$.ClassicProp1", "val1"),
                        new("$.['ClassicProp2']", "val2"),
                        new("$.['Random Property 3!']", "val3"),
                    },
                    ExpectedObject = JObject.Parse(@"
{
    ""ClassicProp1"": ""val1"",
    ""ClassicProp2"": ""val2"",
    ""Random Property 3!"": ""val3""
}
"),
                },
                //["multiply-value"] = new()
                //{
                //    Expressions =
                //    {
                //        new("$.arr[*].val", "text"),
                //    },
                //    ExpectedObject = ,
                //},
            };
        }
    }

    private class UseCase
    {
        public List<UseCaseExpression> Expressions { get; } = new();

        public JObject ExpectedObject { get; init; }

        public void Print()
        {
            Console.WriteLine("Expressions to be executed:");
            var i = 0;
            foreach (var expression in Expressions)
            {
                var tag = string.Empty;
                if (expression.PlacementHint.Any())
                {
                    var tagBody = string.Join(", ", expression.PlacementHint.Select(h => $"{h.Key}:{h.Value}"));
                    tag = $" ({tagBody})";
                }

                Console.WriteLine($"{i,3}: {expression.Path} <= {expression.Value}{tag}");
                i++;
            }

            Console.WriteLine();
            Console.WriteLine("Expected object:");
            Console.WriteLine(ExpectedObject.ToString());
        }

        public class UseCaseExpression
        {
            public UseCaseExpression(string path, string value)
            {
                Path = path;
                Value = value;
            }

            public UseCaseExpression(string path, string value, params (string tag, int? index)[] placementHint)
            {
                Path = path;
                Value = value;
                PlacementHint = placementHint.ToDictionary(h => h.tag, h => h.index);
            }

            public string Path { get; }

            public string Value { get; }

            public Dictionary<string, int?> PlacementHint { get; } = new();
        }
    }
}
