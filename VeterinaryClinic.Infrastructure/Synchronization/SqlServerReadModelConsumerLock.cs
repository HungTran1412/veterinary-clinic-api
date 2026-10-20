using System.Data.Common;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.Data.SqlClient;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public static class SqlServerReadModelConsumerLock
{
    public const string ResourceName = "VeterinaryClinic.ReadModelSynchronization";

    public static async Task<IAsyncDisposable?> AcquireAsync(
        DbConnection connection, Func<Task> closeConnection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = CreateAcquireCommand(connection);
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar == null || scalar == DBNull.Value)
                throw new InvalidOperationException("Không nhận được kết quả lấy khóa đồng bộ.");
            var result = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            if (result == -1)
            {
                await closeConnection();
                return null;
            }
            if (result < 0)
                throw new InvalidOperationException($"Không lấy được khóa đồng bộ (mã {result}).");
            return new Lease(connection, closeConnection);
        }
        catch
        {
            // Đóng session nếu kết quả lấy khóa không rõ; giữ nguyên lỗi ban đầu.
            try { ClearFailedSessionPool(connection); } catch { }
            try { await closeConnection(); } catch { }
            try { await connection.CloseAsync(); } catch { }
            throw;
        }
    }

    public static DbCommand CreateAcquireCommand(DbConnection connection) => CreateCommand(connection, """
        DECLARE @result int;
        EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = N'Exclusive',
            @LockOwner = N'Session', @LockTimeout = 0;
        SELECT @result;
        """);

    public static DbCommand CreateReleaseCommand(DbConnection connection) => CreateCommand(connection, """
        DECLARE @result int;
        EXEC @result = sys.sp_releaseapplock @Resource = @resource, @LockOwner = N'Session';
        SELECT @result;
        """);

    public static void ClearFailedSessionPool(DbConnection connection)
    {
        if (connection is SqlConnection sqlConnection)
            SqlConnection.ClearPool(sqlConnection);
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = ResourceName;
        command.Parameters.Add(parameter);
        return command;
    }

    private sealed class Lease(DbConnection connection, Func<Task> closeConnection) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;
            Exception? error = null;
            try
            {
                await using var command = CreateReleaseCommand(connection);
                var scalar = await command.ExecuteScalarAsync(CancellationToken.None);
                if (scalar == null || scalar == DBNull.Value || Convert.ToInt32(scalar, CultureInfo.InvariantCulture) < 0)
                    throw new InvalidOperationException("Không giải phóng được khóa đồng bộ.");
            }
            catch (Exception exception) { error = exception; }
            finally
            {
                if (error != null)
                {
                    try { ClearFailedSessionPool(connection); } catch { }
                }
                try { await closeConnection(); }
                catch (Exception exception)
                {
                    error ??= exception;
                    try { ClearFailedSessionPool(connection); } catch { }
                }
                if (error != null)
                {
                    try { await connection.CloseAsync(); } catch { }
                }
            }

            if (error != null)
                ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
