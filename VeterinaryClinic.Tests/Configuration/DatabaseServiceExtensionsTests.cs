using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure;
using VeterinaryClinic.Infrastructure.Synchronization;
using VeterinaryClinic.Shared;
using Xunit;

namespace VeterinaryClinic.Tests.Configuration;

public class DatabaseServiceExtensionsTests
{
    private const string WriteConnection = "Server=server;Database=clinic_write;Integrated Security=true";
    private const string ReadConnection = "Server=server;Database=clinic_read;Integrated Security=true";

    [Fact]
    public void SplitConfiguration_SharesWakeupSignalAcrossScopesWithoutConnecting()
    {
        // Kiểm tra request ghi và worker có thể dùng cùng singleton tín hiệu mà DI không mở database.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Func<IContextAccessor>>(() => null!);
        services.AddClinicDatabases(Build(SplitValues()));
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var signal = first.ServiceProvider.GetRequiredService<IReadModelSynchronizationSignal>();
        using var write = first.ServiceProvider.GetRequiredService<VeterinaryClinicDataContext>();
        using var read = second.ServiceProvider.GetRequiredService<VeterinaryClinicReadDataContext>();

        Assert.Same(signal, provider.GetRequiredService<IReadModelSynchronizationSignal>());
        Assert.Same(signal, second.ServiceProvider.GetRequiredService<IReadModelSynchronizationSignal>());
        Assert.Equal(WriteConnection, write.Database.GetConnectionString());
        Assert.Equal(ReadConnection, read.Database.GetConnectionString());
        Assert.Equal(System.Data.ConnectionState.Closed, write.Database.GetDbConnection().State);
        Assert.Equal(System.Data.ConnectionState.Closed, read.Database.GetDbConnection().State);
    }

    // Kiểm tra DI tách đúng hai kết nối, bật capture và đăng ký worker mà không mở DB.
    [Fact]
    public void SplitConfiguration_RegistersIndependentContextsAndWorker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddClinicDatabases(Build(SplitValues()));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var write = new VeterinaryClinicDataContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<VeterinaryClinicDataContext>>(), null!);
        using var read = scope.ServiceProvider.GetRequiredService<VeterinaryClinicReadDataContext>();

        // Assert
        Assert.Equal(WriteConnection, write.Database.GetConnectionString());
        Assert.Equal(ReadConnection, read.Database.GetConnectionString());
        Assert.Equal(QueryTrackingBehavior.NoTracking, read.ChangeTracker.QueryTrackingBehavior);
        Assert.True(provider.GetRequiredService<IOptions<ReadModelSynchronizationOptions>>().Value.Enabled);
        Assert.Contains(services, x => x.ServiceType == typeof(IHostedService)
            && x.ImplementationType == typeof(ReadModelSynchronizationWorker));
        Assert.Equal(System.Data.ConnectionState.Closed, read.Database.GetDbConnection().State);
    }

    // Kiểm tra cấu hình legacy không bật Outbox hoặc đăng ký worker đòi bảng mới.
    [Fact]
    public void LegacyConfiguration_DoesNotEnableSynchronization()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClinicDatabases(Build(new() { ["ConnectionStrings:DefaultConnection"] = WriteConnection }));
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.False(provider.GetRequiredService<IOptions<ReadModelSynchronizationOptions>>().Value.Enabled);
        Assert.DoesNotContain(services, x => x.ImplementationType == typeof(ReadModelSynchronizationWorker));
        Assert.Null(provider.GetService<IReadModelSynchronizationSignal>());
    }

    // Kiểm tra cấu hình sai bị từ chối trước khi mở kết nối hoặc nhận request nghiệp vụ.
    [Theory]
    [InlineData("ReadModelSynchronization:Enabled", "false", "Phải bật")]
    [InlineData("ReadModelSynchronization:BatchSize", "0", "BatchSize")]
    [InlineData("ReadModelSynchronization:BatchSize", "1001", "BatchSize")]
    [InlineData("ReadModelSynchronization:PollIntervalSeconds", "0", "PollIntervalSeconds")]
    [InlineData("ReadModelSynchronization:PollIntervalSeconds", "3601", "PollIntervalSeconds")]
    [InlineData("ConnectionStrings:WriteConnection", WriteConnection + ";MultipleActiveResultSets=true", "MultipleActiveResultSets")]
    [InlineData("ConnectionStrings:ReadConnection", "Server=SERVER;Database=CLINIC_WRITE;User Id=other;Password=secret", "khác nhau")]
    [InlineData("ConnectionStrings:ReadConnection", "Server=server;Integrated Security=true", "Server và Database")]
    public void InvalidConfiguration_FailsBeforeConnecting(string key, string value, string expectedMessage)
    {
        // Arrange
        var values = SplitValues();
        values[key] = value;

        // Act
        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddClinicDatabases(Build(values)));

        // Assert
        Assert.Contains(expectedMessage, error.Message);
        Assert.DoesNotContain("Password=secret", error.ToString());
    }

    // Kiểm tra chế độ đồng bộ không âm thầm lấy kết nối legacy khi thiếu một đích explicit.
    [Theory]
    [InlineData("WriteConnection")]
    [InlineData("ReadConnection")]
    public void Synchronization_RequiresBothExplicitConnections(string missing)
    {
        // Arrange
        var values = SplitValues();
        values.Remove($"ConnectionStrings:{missing}");
        values["ConnectionStrings:DefaultConnection"] = missing == "WriteConnection" ? WriteConnection : ReadConnection;

        // Act
        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddClinicDatabases(Build(values)));

        // Assert
        Assert.Contains("cấu hình rõ", error.Message);
    }

    private static Dictionary<string, string?> SplitValues() => new()
    {
        ["ConnectionStrings:WriteConnection"] = WriteConnection,
        ["ConnectionStrings:ReadConnection"] = ReadConnection,
        ["ReadModelSynchronization:Enabled"] = "true"
    };

    private static IConfiguration Build(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
