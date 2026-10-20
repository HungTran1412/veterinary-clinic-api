using Microsoft.Extensions.Configuration;
using VeterinaryClinic.Shared;
using Xunit;

namespace VeterinaryClinic.Tests.Configuration;

public class DatabaseConnectionSettingsTests
{
    // Kiểm tra cấu hình cũ vẫn định tuyến cả đọc và ghi về DefaultConnection.
    [Fact]
    public void LegacyConfiguration_UsesDefaultConnectionForBothContexts()
    {
        // Arrange
        var config = Build(new() { ["ConnectionStrings:DefaultConnection"] = "legacy" });

        // Act
        var write = DatabaseConnectionSettings.GetWriteConnectionString(config);
        var read = DatabaseConnectionSettings.GetReadConnectionString(config);

        // Assert
        Assert.Equal("legacy", write);
        Assert.Equal("legacy", read);
    }

    // Kiểm tra kết nối tách biệt luôn được ưu tiên hơn cấu hình legacy còn sót lại.
    [Fact]
    public void ExplicitConnections_OverrideLegacyConnection()
    {
        // Arrange
        var config = Build(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "legacy",
            ["ConnectionStrings:WriteConnection"] = "write",
            ["ConnectionStrings:ReadConnection"] = "read"
        });

        // Act
        var write = DatabaseConnectionSettings.GetWriteConnectionString(config);
        var read = DatabaseConnectionSettings.GetReadConnectionString(config);

        // Assert
        Assert.Equal("write", write);
        Assert.Equal("read", read);
    }

    // Kiểm tra kết nối trống không được dùng và phải báo rõ tên cấu hình thiếu.
    [Theory]
    [InlineData(false, "ReadConnection")]
    [InlineData(true, "WriteConnection")]
    public void MissingConnection_ThrowsConfigurationError(bool write, string expectedName)
    {
        // Arrange
        var config = Build(new() { ["ConnectionStrings:DefaultConnection"] = " " });

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => write
            ? DatabaseConnectionSettings.GetWriteConnectionString(config)
            : DatabaseConnectionSettings.GetReadConnectionString(config));

        // Assert
        Assert.Contains(expectedName, error.Message);
    }

    private static IConfiguration Build(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
