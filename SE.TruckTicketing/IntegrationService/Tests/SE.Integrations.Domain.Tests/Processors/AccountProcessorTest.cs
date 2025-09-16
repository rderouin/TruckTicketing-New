using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class AccountProcessorTest
{
    private Mock<IManager<Guid, AccountEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private AccountProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new AccountProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task Account_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var account = GenFu.GenFu.New<Account>();
        var accountContact = GenFu.GenFu.New<AccountContact>();
        account.Contacts = new() { accountContact };
        var entityEnvelop = new EntityEnvelopeModel<Account>();
        entityEnvelop.Payload = account;

        Exception exc = null;

        //act
        try
        {
            await _processor.Process(entityEnvelop);
        }
        catch (Exception ex)
        {
            exc = ex;
        }

        //assert
        exc.Should().BeNull();
    }
}
