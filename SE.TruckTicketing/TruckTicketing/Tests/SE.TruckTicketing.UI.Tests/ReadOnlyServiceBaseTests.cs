using System;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Moq;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

using Xunit;

namespace SE.TruckTicketing.UI.Tests;

public class ReadOnlyServiceBaseTests
{
    [Fact]
    public async Task GetById_UseCacheFalse_ShouldReturnFromApi()
    {
        var service = new GetByIdServiceStub();
        var originalEntity = service.TestEntity;

        var result = await service.GetById(originalEntity.Id);
        result.Should().Be(originalEntity);

        service.UpdateEntity();

        var newResult = await service.GetById(originalEntity.Id);
        newResult.Should().NotBe(originalEntity);
        newResult.Should().Be(service.TestEntity);
    }

    [Fact]
    public async Task GetById_UseCacheAndInCache_ShouldReturnCachedData()
    {
        HttpServiceBaseBase.DisableCache = false; // TODO: NOTE - this line needs to be deleted once caching is re-enabled
        var service = new GetByIdServiceStub();
        var originalEntity = service.TestEntity;

        var result = await service.GetById(originalEntity.Id, true);
        result.Should().Be(originalEntity);

        service.UpdateEntity();

        var cachedResult = await service.GetById(originalEntity.Id, true);
        cachedResult.Should().Be(originalEntity);
    }

    [Fact]
    public async Task GetById_UseCacheFalse_ShouldInvalidateCache()
    {
        var service = new GetByIdServiceStub();
        var originalEntity = service.TestEntity;
        
        var result = await service.GetById(originalEntity.Id, true);
        result.Should().Be(originalEntity);

        service.UpdateEntity();

        var newResult = await service.GetById(originalEntity.Id, false);
        newResult.Should().Be(service.TestEntity);
        newResult.Should().NotBe(originalEntity);
    }
    
    [Fact]
    public async Task GetById_CustomCacheOptions_ShouldApplyCustomOptions()
    {
        HttpServiceBaseBase.DisableCache = false; // TODO: NOTE - this line needs to be deleted once caching is re-enabled
        var service = new GetByIdServiceStub();
        var originalEntity = service.TestEntity;

        var customConfigCalled = false;
        Action<ICacheEntry> customConfig = entry => { customConfigCalled = true; };
        await service.GetById(originalEntity.Id, true, customConfig);
        customConfigCalled.Should().BeTrue();
    }

    [Service("TestService", "TestsEndpoint")]
    public class GetByIdServiceStub : ReadOnlyServiceBase<GetByIdServiceStub, TestEntity, Guid>
    {
        public GetByIdServiceStub() : base(new Mock<ILogger<GetByIdServiceStub>>().Object, new Mock<IHttpClientFactory>().Object)
        {
            TestEntity = new();
        }

        public TestEntity TestEntity { get; set; }

        protected override Task<Response<T>> SendRequest<T>(string method, string route, object data = null)
        {
            var response = new Response<TestEntity> { Model = TestEntity };
            return Task.FromResult(response) as Task<Response<T>>;
        }

        public void UpdateEntity()
        {
            TestEntity = new TestEntity()
            {
                Id = TestEntity.Id,
                Data = DateTime.UtcNow.Ticks
            };
        }
    }

    public class TestEntity : IGuidModelBase
    {
        public TestEntity()
        {
            Id = Guid.NewGuid();
            Data = DateTime.UtcNow.Ticks;
        }

        public long Data { get; set; }

        public Guid Id { get; set; }
    }
}
