using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using FluentAssertions;

using Humanizer;

using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Changes;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts.Configuration;
using Trident.Data;
using Trident.Domain;
using Trident.Extensions;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Entities.Changes;

[TestClass]
public class ChangeComparerTests
{
    [TestMethod]
    public void Compare_NoChanges()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void Compare_PropertyChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        testData.target.Name = "New Value";
        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(nameof(testData.source.Name), changes[0].FieldName);
        Assert.AreEqual(ChangeOperation.Updated, changes[0].Operation);
        Assert.AreEqual(testData.target.Name, changes[0].ValueAfter);
        Assert.AreEqual(testData.source.Name, changes[0].ValueBefore);
        Assert.AreEqual(string.Empty, changes[0].FieldLocation);
    }

    [TestMethod]
    public void Compare_PrimitiveCollections()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        testData.source.EnrolledSubjects.Insert(0, "Another Random Subject");
        testData.target.EnrolledSubjects.Add("New Subject");
        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(2, changes.Count);

        var addedItem = changes[0];
        Assert.AreEqual(ChangeOperation.Added, addedItem.Operation);
        Assert.AreEqual("EnrolledSubjects[3]", addedItem.FieldLocation);
        Assert.AreEqual("", addedItem.FieldName);
        Assert.AreEqual(null, addedItem.ValueBefore);
        Assert.AreEqual("New Subject", addedItem.ValueAfter);

        var deletedItem = changes[1];
        Assert.AreEqual(ChangeOperation.Deleted, deletedItem.Operation);
        Assert.AreEqual("EnrolledSubjects[0]", deletedItem.FieldLocation);
        Assert.AreEqual("", deletedItem.FieldName);
        Assert.AreEqual("Another Random Subject", deletedItem.ValueBefore);
        Assert.AreEqual(null, deletedItem.ValueAfter);
    }

    [TestMethod]
    public void Compare_NonPrimitiveCollection()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var newChildElementSource = scope.NewChildItem(8);
        var newChildElementTarget = scope.NewChildItem(9);
        testData.source.ChildItems.Insert(0, newChildElementSource);
        testData.target.ChildItems.Add(newChildElementTarget);
        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        var other = changes.Where(c => c.Operation is not (ChangeOperation.Added or ChangeOperation.Deleted)).ToList();
        other.Count.Should().Be(0);

        var addedList = changes.Where(c => c.Operation == ChangeOperation.Added).ToList();
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Street));
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.City));
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Province));
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.ZipCode));
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Country));
        addedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Id));
        addedList.Should().Contain(c => c.FieldName == nameof(PrimitiveCollection<object>.Key));
        addedList.Should().Contain(c => c.FieldName == nameof(PrimitiveCollection<object>.Raw));
        addedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.Id));
        addedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.SignatoryType));
        addedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.IsActive));
        addedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.Name));
        addedList.Should().Contain(c => c.FieldLocation == "ChildItems[5].Sports[0]");
        addedList.Should().Contain(c => c.FieldLocation == "ChildItems[5].Sports[1]");

        var deletedList = changes.Where(c => c.Operation == ChangeOperation.Deleted).ToList();
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Street));
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.City));
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Province));
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.ZipCode));
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Country));
        deletedList.Should().Contain(c => c.FieldName == nameof(AddressEntity.Id));
        deletedList.Should().Contain(c => c.FieldName == nameof(PrimitiveCollection<object>.Key));
        deletedList.Should().Contain(c => c.FieldName == nameof(PrimitiveCollection<object>.Raw));
        deletedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.Id));
        deletedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.SignatoryType));
        deletedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.IsActive));
        deletedList.Should().Contain(c => c.FieldName == nameof(ChildEntity.Name));
        deletedList.Should().Contain(c => c.FieldLocation == "ChildItems[0].Sports[0]");
        deletedList.Should().Contain(c => c.FieldLocation == "ChildItems[0].Sports[1]");
    }

    [TestMethod]
    public void Compare_NonPrimitiveCollection_ObjectUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var updatedChild = testData.target.ChildItems[0];
        updatedChild.Name = "another name";
        updatedChild.ChildAddress = new()
        {
            City = "another city",
            Country = CountryCode.CA,
            Province = StateProvince.ON,
            Street = "another street",
            Id = Guid.NewGuid(),
            ZipCode = "actually a postal code",
        };

        updatedChild.Sports = new()
        {
            "Chess-0",
            "Tennis-0",
            "Cricket",
        };

        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        var sourceChildItem = testData.source.ChildItems[0];
        var childItemAddressCount = JObject.FromObject(testData.source.ChildItems[0].ChildAddress).Properties().Count();
        Assert.AreEqual(childItemAddressCount + 2, changes.Count);
        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.ChildAddress.City) && x.Operation == ChangeOperation.Updated &&
                                       x.ValueBefore == sourceChildItem.ChildAddress.City && x.ValueAfter == updatedChild.ChildAddress.City));

        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.ChildAddress.Country) && x.Operation == ChangeOperation.Updated &&
                                       x.ValueBefore == sourceChildItem.ChildAddress.Country.ToString() && x.ValueAfter == updatedChild.ChildAddress.Country.ToString()));

        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.ChildAddress.ZipCode) && x.Operation == ChangeOperation.Updated &&
                                       x.ValueBefore == sourceChildItem.ChildAddress.ZipCode && x.ValueAfter == updatedChild.ChildAddress.ZipCode));

        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.ChildAddress.Province) && x.Operation == ChangeOperation.Updated &&
                                       x.ValueBefore == sourceChildItem.ChildAddress.Province.ToString() && x.ValueAfter == updatedChild.ChildAddress.Province.ToString()));

        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.ChildAddress.Street) && x.Operation == ChangeOperation.Updated &&
                                       x.ValueBefore == sourceChildItem.ChildAddress.Street && x.ValueAfter == updatedChild.ChildAddress.Street));

        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(updatedChild.Name)));
        Assert.IsTrue(changes.Any(x => x.FieldLocation == "ChildItems[0].Sports[2]"));
        Assert.IsTrue(changes.Any(x => x.Operation == ChangeOperation.Added && x.ValueAfter == "Cricket" && x.FieldLocation == "ChildItems[0].Sports[2]"));
    }

    [TestMethod]
    public void Compare_NonPrimitiveCollection_MultipleChanges()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        //Updated existing element
        var updatedChild = testData.target.ChildItems[0];
        updatedChild.Name = "another name";
        updatedChild.ChildAddress = new()
        {
            City = "another city",
            Country = CountryCode.CA,
            Province = StateProvince.ON,
            Street = "another street",
            Id = Guid.NewGuid(),
            ZipCode = "actually a postal code",
        };

        updatedChild.Sports = new()
        {
            "Chess",
            "Tennis",
            "Cricket",
        };

        //Remove existing element
        var removedChildItem = testData.target.ChildItems[^1];
        testData.target.ChildItems.Remove(removedChildItem);

        //Added new element
        var newChildElement = scope.NewChildItem(7);
        newChildElement.Sports = new()
        {
            "Football",
            "Baseball",
            "Basketball",
        };

        testData.target.ChildItems.Add(newChildElement);

        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        var allDeletedLogs = changes.Where(x => x.Operation == ChangeOperation.Deleted).ToList();
        var allAddedLogs = changes.Where(x => x.Operation == ChangeOperation.Added).ToList();
        var allUpdatedLogs = changes.Where(x => x.Operation == ChangeOperation.Updated).ToList();
        var childAddressProperties = typeof(AddressEntity).GetProperties();
        var childElementProperties = typeof(ChildEntity).GetProperties();
        //Added log assert
        foreach (var childAddressProperty in childAddressProperties)
        {
            Assert.IsTrue(allAddedLogs.Any(x => x.FieldName == childAddressProperty.Name));
            Assert.IsTrue(allDeletedLogs.Any(x => x.FieldName == childAddressProperty.Name));
            Assert.IsTrue(allUpdatedLogs.Any(x => x.FieldName == childAddressProperty.Name));
        }

        foreach (var childElementProperty in childElementProperties)
        {
            if (childElementProperty.PropertyType == typeof(AddressEntity))
            {
                foreach (var childAddressProperty in childAddressProperties)
                {
                    Assert.IsTrue(allAddedLogs.Any(x => x.FieldName == childAddressProperty.Name));
                    Assert.IsTrue(allDeletedLogs.Any(x => x.FieldName == childAddressProperty.Name));
                }

                continue;
            }

            if (childElementProperty.PropertyType == typeof(PrimitiveCollection<string>))
            {
                continue;
            }

            if (childElementProperty.PropertyType == typeof(List<string>))
            {
                continue;
            }

            Assert.IsTrue(allAddedLogs.Any(x => x.FieldName == childElementProperty.Name));
            Assert.IsTrue(allDeletedLogs.Any(x => x.FieldName == childElementProperty.Name));
        }

        foreach (var newlyAddedSports in newChildElement.Sports)
        {
            Assert.IsTrue(allAddedLogs.Any(x => x.ValueAfter == newlyAddedSports));
        }

        Assert.IsTrue(allUpdatedLogs.Any(x => x.FieldName == nameof(newChildElement.Name)));
        Assert.IsTrue(changes.Any(x => x.Operation == ChangeOperation.Added && x.ValueAfter == "Cricket" && x.FieldLocation == "ChildItems[0].Sports[2]"));
        Assert.IsTrue(allAddedLogs.All(x => string.IsNullOrEmpty(x.ValueBefore)));
        Assert.IsTrue(allDeletedLogs.All(x => string.IsNullOrEmpty(x.ValueAfter)));

        Assert.IsTrue(testData.source.ChildItems.All(x => x.Id != newChildElement.Id) && testData.target.ChildItems.Any(x => x.Id == newChildElement.Id));
        Assert.IsTrue(testData.source.ChildItems.Any(x => x.Id == removedChildItem.Id) && testData.target.ChildItems.All(x => x.Id != removedChildItem.Id));
    }

    [TestMethod]
    public void Compare_SourceNull()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var target = scope.AsJObject(testData.target);

        // act
        var changes = scope.InstanceUnderTest.Compare(null, target);

        // assert
        AssertChangeLogs(changes);
        Assert.IsTrue(changes.All(x => x.Operation == ChangeOperation.Added));
    }

    [TestMethod]
    public void Compare_TargetNull()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var source = scope.AsJObject(testData.source);

        // act
        var changes = scope.InstanceUnderTest.Compare(source, null);

        // assert
        AssertChangeLogs(changes);
        Assert.IsTrue(changes.All(x => x.Operation == ChangeOperation.Deleted));
    }

    [TestMethod]
    public void Compare_NewPropertyOnTarget()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var source = scope.AsJObject(testData.source);
        var target = scope.AsJObject(testData.target);
        target.Add(new JProperty(nameof(TTAuditableEntityBase.CreatedBy), "NewPropertyAdded"));
        foreach (var childElement in target["ChildItems"]!)
        {
            var elementObject = (JObject)childElement;
            elementObject.Add(new JProperty(nameof(TTAuditableEntityBase.DocumentType), "ChildPropertyAdded"));
        }

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.IsTrue(changes.All(x => x.Operation == ChangeOperation.Added));
        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(TTAuditableEntityBase.CreatedBy)));
        Assert.IsTrue(changes.Count(x => x.FieldName == nameof(TTAuditableEntityBase.DocumentType)) == testData.target.ChildItems.Count);
        Assert.IsTrue(changes.All(x => string.IsNullOrEmpty(x.ValueBefore)));
        Assert.IsTrue(changes.Any(x => x.ValueAfter == "NewPropertyAdded"));
        Assert.IsTrue(changes.Any(x => x.ValueAfter == "ChildPropertyAdded"));
    }

    [TestMethod]
    public void Compare_PropertyDeleted()
    {
        // arrange
        var scope = new DefaultScope();
        var testData = scope.GetTestData();
        var source = scope.AsJObject(testData.source);
        var target = (JObject)scope.AsJObject(testData.target).DeepClone();
        target.Properties().First(x => x.Name == "Name").Remove();

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(1, changes.Count);
        Assert.IsTrue(changes.Any(x => x.FieldName == nameof(TestEntity.Name)));
        Assert.IsTrue(changes.All(x => x.Operation == ChangeOperation.Deleted));
        Assert.IsTrue(changes.Any(x => x.ValueBefore == testData.source.Name));
        Assert.IsTrue(changes.Any(x => string.IsNullOrEmpty(x.ValueAfter)));
    }

    [TestMethod]
    public void Compare_HasRefId()
    {
        // arrange
        var scope = new DefaultScope();
        var testObj = new
        {
            RandomProperty = "RandomValue",
        };

        var source = JObject.FromObject(testObj);
        var target = JObject.FromObject(testObj);
        target["RandomProperty"] = "AnotherRandomValue";

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("RandomProperty", changes[0].FieldName);
        Assert.AreEqual(null, changes[0].ObjectId);
    }

    [TestMethod]
    public void Compare_NoRefId()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var testObj = new
        {
            Id = id,
            RandomProperty = "RandomValue",
        };

        var source = JObject.FromObject(testObj);
        var target = JObject.FromObject(testObj);
        target["RandomProperty"] = "AnotherRandomValue";

        // act
        var changes = scope.InstanceUnderTest.Compare(source, target);

        // assert
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("RandomProperty", changes[0].FieldName);
        Assert.AreEqual(id.ToString(), changes[0].ObjectId);
    }

    [TestMethod]
    public void Compare_DuplicateObjectArrayEntries()
    {
        // arrange
        var scope = new DefaultScope();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var objFactory = (string suffix) => new
        {
            Arr = new[]
            {
                new
                {
                    Id = id1,
                    Value = $"val1{suffix}",
                },
                new
                {
                    Id = id1,
                    Value = $"val2{suffix}",
                },
                new
                {
                    Id = id2,
                    Value = $"val3{suffix}",
                },
                new
                {
                    Id = id1,
                    Value = $"val4{suffix}",
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Value = $"val5{suffix}",
                },
            },
        };

        var j1 = JObject.FromObject(objFactory("-left"));
        var j2 = JObject.FromObject(objFactory("-right"));
        var permutations = new (string before, string after)[]
        {
            ("val1-left", "val1-right"),
            ("val1-left", "val2-right"),
            ("val1-left", "val4-right"),
            ("val2-left", "val1-right"),
            ("val2-left", "val2-right"),
            ("val2-left", "val4-right"),
            ("val4-left", "val1-right"),
            ("val4-left", "val2-right"),
            ("val4-left", "val4-right"),
            ("val3-left", "val3-right"),
            ("val5-left", null),
            (null, "val5-right"),
        };

        // act
        var changes = scope.InstanceUnderTest.Compare(j1, j2);

        // assert
        changes.Count.Should().Be(14);
        foreach (var permutation in permutations)
        {
            changes.Any(change => change.ValueBefore == permutation.before && change.ValueAfter == permutation.after).Should().BeTrue();
        }
    }

    private void AssertChangeLogs(List<FieldChange> changes)
    {
        Regex arrayIndexRegex = new(@"\[\d+\]", RegexOptions.Compiled);
        var entityProperties = typeof(TestEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var childAddressProperties = typeof(AddressEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var childElementProperties = typeof(ChildEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var property in entityProperties)
        {
            if (property.PropertyType == typeof(List<ChildEntity>))
            {
                foreach (var childElementProperty in childElementProperties)
                {
                    if (childElementProperty.PropertyType == typeof(PrimitiveCollection<string>))
                    {
                        continue;
                    }

                    if (childElementProperty.PropertyType == typeof(AddressEntity))
                    {
                        foreach (var addressProperty in childAddressProperties)
                        {
                            Assert.IsTrue(changes.Any(x => x.FieldName == addressProperty.Name));
                        }

                        continue;
                    }

                    if (childElementProperty.PropertyType == typeof(List<string>))
                    {
                        Assert.IsTrue(changes.Any(x => arrayIndexRegex.Replace(x.FieldLocation, "").Split(".")[^1] == property.Name));
                        continue;
                    }

                    Assert.IsTrue(changes.Any(x => x.FieldName == childElementProperty.Name));
                }

                continue;
            }

            if (property.PropertyType == typeof(PrimitiveCollection<string>))
            {
                continue;
            }

            if (property.PropertyType == typeof(List<string>) || property.PropertyType == typeof(List<Guid>))
            {
                Assert.IsTrue(changes.Any(x => arrayIndexRegex.Replace(x.FieldLocation, "").Split(".")[^1] == property.Name));
                continue;
            }

            Assert.IsTrue(changes.Any(x => x.FieldName == property.Name));
            Assert.IsTrue(changes.Any(x => x.FieldName == property.Name));
        }
    }

    private class DefaultScope : TestScope<ChangeComparer>
    {
        public DefaultScope()
        {
            AppSettings.Setup(o => o.GetKeyOrDefault(It.IsAny<string>(), It.IsAny<string>())).Returns("Id");
            InstanceUnderTest = new(AppSettings.Object, Logger.Object);
        }

        public Mock<IAppSettings> AppSettings { get; } = new();

        public Mock<ILog> Logger { get; } = new();

        public (TestEntity source, TestEntity target) GetTestData(int childItemCount = 5)
        {
            var source = new TestEntity
            {
                Id = Guid.NewGuid(),
                Name = "name",
                Description = "description",
                ChildItems = Enumerable.Range(0, childItemCount).Select(NewChildItem).ToList(),
                EnrolledSubjects =
                {
                    "Math",
                    "Science",
                    "History",
                },
                SubjectIds = new()
                {
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                },
                EntityTypes = new()
                {
                    List = new()
                    {
                        "type1",
                        "type2",
                    },
                },
            };

            var target = source.Clone();

            return (source, target);
        }

        public ChildEntity NewChildItem(int i)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Name = $"child-name-{i}",
                Sports = new()
                {
                    $"Chess-{i}",
                    $"Tennis-{i}",
                },
                ChildAddress = new()
                {
                    Id = Guid.NewGuid(),
                    City = $"city-{i}",
                    Country = CountryCode.US,
                    Province = StateProvince.NY,
                    ZipCode = $"zip-{i}",
                    Street = $"street-{i}",
                },
            };
        }

        public JObject AsJObject(object entity)
        {
            return JObject.FromObject(entity, JsonSerializer.CreateDefault(new() { Converters = { new StringEnumConverter() } }));
        }
    }

    [Container("Test", nameof(DocumentType), "Test", PartitionKeyType.WellKnown)]
    [Discriminator(nameof(EntityType), "Test")]
    private class TestEntity : TTEntityBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        [OwnedHierarchy]
        public PrimitiveCollection<string> EntityTypes { get; set; } = new();

        [OwnedHierarchy]
        public List<ChildEntity> ChildItems { get; set; } = new();

        public List<string> EnrolledSubjects { get; } = new();

        public List<Guid> SubjectIds { get; set; } = new();
    }

    private class ChildEntity : OwnedEntityBase<Guid>
    {
        public bool IsActive { get; set; }

        public string Name { get; set; }

        public AddressEntity ChildAddress { get; set; }

        [OwnedHierarchy]
        public PrimitiveCollection<string> Functions { get; } = new();

        public List<string> Sports { get; set; } = new();

        public Types SignatoryType { get; set; }
    }

    private class AddressEntity : OwnedEntityBase<Guid>
    {
        public string Street { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        public CountryCode Country { get; set; }

        public StateProvince Province { get; set; }

        public string Format()
        {
            var line1 = $"{Street}";
            var line2 = $"{City}, {Province}, {ZipCode}";
            var line3 = $"{Country.Humanize()}";
            return string.Join(Environment.NewLine, line1, line2, line3);
        }
    }

    private enum Types
    {
        [System.ComponentModel.Description("")]
        None = default,

        Savings,

        Checking,
    }
}
