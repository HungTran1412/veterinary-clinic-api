using Microsoft.Extensions.Configuration;

namespace VeterinaryClinic.Shared;

public static class DatabaseConnectionSettings
{
    public static string GetWriteConnectionString(IConfiguration configuration)
        => Resolve(configuration, "WriteConnection");

    public static string GetReadConnectionString(IConfiguration configuration)
        => Resolve(configuration, "ReadConnection");

    private static string Resolve(IConfiguration configuration, string name)
    {
        var connection = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connection))
        {
            connection = configuration.GetConnectionString("DefaultConnection");
        }

        return !string.IsNullOrWhiteSpace(connection)
            ? connection
            : throw new InvalidOperationException($"Chưa cấu hình ConnectionStrings:{name} hoặc DefaultConnection.");
    }
}
