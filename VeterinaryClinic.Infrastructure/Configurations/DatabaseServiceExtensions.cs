using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure.Synchronization;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Infrastructure;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddClinicDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        var writeConnection = DatabaseConnectionSettings.GetWriteConnectionString(configuration);
        var readConnection = DatabaseConnectionSettings.GetReadConnectionString(configuration);
        var synchronization = configuration.GetSection(ReadModelSynchronizationOptions.SectionName)
            .Get<ReadModelSynchronizationOptions>() ?? new ReadModelSynchronizationOptions();

        Validate(configuration, writeConnection, readConnection, synchronization);

        services.Configure<ReadModelSynchronizationOptions>(
            configuration.GetSection(ReadModelSynchronizationOptions.SectionName));
        services.AddDbContext<VeterinaryClinicDataContext>(options => options.UseSqlServer(writeConnection));
        services.AddDbContext<VeterinaryClinicReadDataContext>(options => options
            .UseSqlServer(readConnection).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        if (synchronization.Enabled)
        {
            services.AddReadModelSynchronization();
        }

        return services;
    }

    private static void Validate(IConfiguration configuration, string writeConnection, string readConnection,
        ReadModelSynchronizationOptions synchronization)
    {
        SqlConnectionStringBuilder write;
        SqlConnectionStringBuilder read;
        try
        {
            write = new SqlConnectionStringBuilder(writeConnection);
            read = new SqlConnectionStringBuilder(readConnection);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Chuỗi kết nối SQL Server đọc hoặc ghi không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(write.DataSource) || string.IsNullOrWhiteSpace(write.InitialCatalog)
            || string.IsNullOrWhiteSpace(read.DataSource) || string.IsNullOrWhiteSpace(read.InitialCatalog))
        {
            throw new InvalidOperationException("Kết nối đọc và ghi phải chỉ rõ Server và Database.");
        }

        var sameDatabase = string.Equals(write.DataSource.Trim(), read.DataSource.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(write.InitialCatalog.Trim(), read.InitialCatalog.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!synchronization.Enabled)
        {
            if (!sameDatabase)
            {
                throw new InvalidOperationException("Phải bật ReadModelSynchronization:Enabled khi tách database đọc và ghi.");
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("WriteConnection"))
            || string.IsNullOrWhiteSpace(configuration.GetConnectionString("ReadConnection")))
        {
            throw new InvalidOperationException("Đồng bộ yêu cầu cấu hình rõ WriteConnection và ReadConnection.");
        }
        if (sameDatabase)
        {
            throw new InvalidOperationException("Đồng bộ yêu cầu hai database đọc và ghi khác nhau.");
        }
        if (write.MultipleActiveResultSets)
        {
            throw new InvalidOperationException("WriteConnection phải tắt MultipleActiveResultSets để Outbox sử dụng savepoint.");
        }
        if (synchronization.PollIntervalSeconds is < 1 or > 3600 || synchronization.BatchSize is < 1 or > 1000)
        {
            throw new InvalidOperationException("PollIntervalSeconds phải từ 1 đến 3600, BatchSize phải từ 1 đến 1000.");
        }
    }
}
