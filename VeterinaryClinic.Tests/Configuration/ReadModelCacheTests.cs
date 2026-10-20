using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using VeterinaryClinic.Business;
using Xunit;

namespace VeterinaryClinic.Tests.Configuration;

public class ReadModelCacheTests
{
    // Kiểm tra cả hai overload luôn lấy dữ liệu mới khi bật đồng bộ, dù cache còn bản cũ.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetOrCreate_WhenSynchronizationEnabled_BypassesStaleCache(bool asynchronousFactory)
    {
        // Arrange
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("\"cũ\""));
        var service = CreateService(cache.Object, true);

        // Act
        var actual = asynchronousFactory
            ? await service.GetOrCreate("user", () => Task.FromResult("mới"))
            : await service.GetOrCreate("user", () => "mới");

        // Assert
        Assert.Equal("mới", actual);
        cache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Kiểm tra hệ thống một DB vẫn đọc cache và không gọi factory khi đã có dữ liệu.
    [Fact]
    public async Task GetOrCreate_WhenSynchronizationDisabled_UsesExistingCache()
    {
        // Arrange
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("\"đã lưu\""));
        var service = CreateService(cache.Object, false);
        var factoryCalled = false;

        // Act
        var actual = await service.GetOrCreate("user", () =>
        {
            factoryCalled = true;
            return "mới";
        });

        // Assert
        Assert.Equal("đã lưu", actual);
        Assert.False(factoryCalled);
    }

    // Kiểm tra mọi overload Set không ghi kết quả có thể cũ vào Redis khi đang đồng bộ.
    [Fact]
    public async Task Set_WhenSynchronizationEnabled_DoesNotPopulateCache()
    {
        // Arrange
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var service = CreateService(cache.Object, true);

        // Act
        await service.Set("user", "mới");
        await service.Set("user", () => "mới");
        await service.Set("user", () => Task.FromResult("mới"));

        // Assert
        cache.VerifyNoOtherCalls();
    }

    private static RedisCacheService CreateService(IDistributedCache cache, bool synchronizationEnabled)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:EnableCache"] = "true",
            ["ReadModelSynchronization:Enabled"] = synchronizationEnabled.ToString()
        }).Build();
        return new RedisCacheService(cache, config);
    }
}
