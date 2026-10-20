using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using VeterinaryClinic.Data;
using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class ProjectionSqlTests
{
    [Fact]
    public void CreateInsert_UsesMappedIdentifiersAndParameterizesBusinessValues()
    {
        // Kiểm tra SQL Server giữ khóa identity trong INSERT và dữ liệu chứa SQL chỉ đi qua parameter.
        using var context = SqlServerContext();
        var name = "'; DROP TABLE vcSpecializations; --";
        var state = Snapshot(context, new VcSpecializations
        {
            Id = 47, Code = "SP47", Name = name, CreatedUserName = "Người tạo", ModifiedUserName = "Người sửa"
        });
        var sql = new ReadModelProjectionSql(context);

        using var command = sql.CreateInsert(state);

        Assert.Contains("INSERT INTO [vcSpecializations]", command.CommandText);
        Assert.Contains("[id]", command.CommandText);
        Assert.DoesNotContain(name, command.CommandText);
        Assert.Contains(command.Parameters.Cast<DbParameter>(), p => Equals(p.Value, name));
        Assert.Contains(command.Parameters.Cast<DbParameter>(), p => Equals(p.Value, 47));
        Assert.True(sql.HasIdentity(context.Model.FindEntityType(typeof(VcSpecializations))!));
    }

    [Fact]
    public void CreateUpdate_ExcludesPrimaryKeyFromAssignments()
    {
        // Kiểm tra UPDATE không sửa cột identity nhưng vẫn định vị bản ghi bằng khóa nguồn.
        using var context = SqlServerContext();
        var state = Snapshot(context, new VcSpecializations
        {
            Id = 47, Code = "SP47", Name = "Chuyên khoa", CreatedUserName = "Người tạo", ModifiedUserName = "Người sửa"
        });

        using var command = new ReadModelProjectionSql(context).CreateUpdate(state)!;

        Assert.DoesNotContain("[id] =", command.CommandText.Split(" WHERE ")[0]);
        Assert.Contains("WHERE [id] = @p", command.CommandText);
        Assert.Contains("[name] = @p", command.CommandText);
    }

    [Fact]
    public void CompositeKeyOnlyRow_HasNoUpdateAndUsesEveryKeyInDelete()
    {
        // Kiểm tra bảng chỉ gồm khóa ghép không có UPDATE vô nghĩa và DELETE ràng buộc đủ hai cột.
        using var context = SqlServerContext();
        var state = Snapshot(context, new VcDoctorSpecializations { DoctorId = 7, SpecializationId = 3 });
        var sql = new ReadModelProjectionSql(context);

        Assert.Null(sql.CreateUpdate(state));
        using var command = sql.CreateDelete(state with { Values = null });

        Assert.Contains("[doctor_id] = @p0 AND [specialization_id] = @p1", command.CommandText);
        Assert.Equal(new object[] { 7, 3 }, command.Parameters.Cast<DbParameter>().Select(p => p.Value));
    }

    [Fact]
    public void CreateIdentityInsert_OnlyTargetsMappedTable()
    {
        // Kiểm tra ON/OFF IDENTITY_INSERT dùng tên bảng từ metadata và cùng connection replica.
        using var context = SqlServerContext();
        var sql = new ReadModelProjectionSql(context);
        var type = context.Model.FindEntityType(typeof(VcSpecializations))!;

        using var enable = sql.CreateIdentityInsert(type, true);
        using var disable = sql.CreateIdentityInsert(type, false);

        Assert.Equal("SET IDENTITY_INSERT [vcSpecializations] ON", enable.CommandText);
        Assert.Equal("SET IDENTITY_INSERT [vcSpecializations] OFF", disable.CommandText);
        Assert.Same(context.Database.GetDbConnection(), enable.Connection);
    }

    [Fact]
    public void CreateLockCommands_UseExclusiveSessionLockAndBoundedAcquisition()
    {
        // Kiểm tra lệnh khóa serialize API nodes bằng session nguồn, timeout0 và resource parameter cố định.
        using var connection = new SqlConnection();
        using var acquire = SqlServerReadModelConsumerLock.CreateAcquireCommand(connection);
        using var release = SqlServerReadModelConsumerLock.CreateReleaseCommand(connection);

        Assert.Contains("sys.sp_getapplock", acquire.CommandText);
        Assert.Contains("@LockMode = N'Exclusive'", acquire.CommandText);
        Assert.Contains("@LockOwner = N'Session'", acquire.CommandText);
        Assert.Contains("@LockTimeout = 0", acquire.CommandText);
        Assert.Equal(SqlServerReadModelConsumerLock.ResourceName, acquire.Parameters["@resource"].Value);
        Assert.Contains("sys.sp_releaseapplock", release.CommandText);
        Assert.Equal(SqlServerReadModelConsumerLock.ResourceName, release.Parameters["@resource"].Value);
    }

    [Fact]
    public async Task ConsumerLease_WhenCallerCancelled_ReleasesWithUncancelledTokenAndCloses()
    {
        // Kiểm tra token caller đã hủy vẫn giải phóng application lock bằng token cleanup độc lập.
        using var cancellation = new CancellationTokenSource();
        var acquire = Command(0);
        var release = Command(0);
        CancellationToken releaseToken = cancellation.Token;
        release.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => releaseToken = token).ReturnsAsync((object)0);
        var connection = Connection(acquire, release);
        var closed = 0;
        var lease = await SqlServerReadModelConsumerLock.AcquireAsync(connection.Object,
            () => { closed++; return Task.CompletedTask; }, cancellation.Token);
        cancellation.Cancel();

        await lease!.DisposeAsync();

        Assert.False(releaseToken.CanBeCanceled);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task ConsumerLease_WhenReleaseAndCloseFail_PreservesReleaseErrorAndForcesClose()
    {
        // Kiểm tra lỗi trả khóa vẫn buộc đóng session và không bị lỗi close thứ cấp che lấp.
        var original = new IOException("release failed");
        var release = Command(0);
        release.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ThrowsAsync(original);
        var connection = Connection(Command(0), release);
        var lease = await SqlServerReadModelConsumerLock.AcquireAsync(connection.Object,
            () => throw new IOException("close failed"), default);

        var error = await Assert.ThrowsAsync<IOException>(() => lease!.DisposeAsync().AsTask());

        Assert.Same(original, error);
        connection.Verify(x => x.CloseAsync(), Times.Once);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-3)]
    [InlineData(-999)]
    public async Task AcquireLock_WhenSqlServerReturnsFailure_FailsClosedAndCloses(int result)
    {
        // Kiểm tra mọi mã lỗi ngoài timeout giữ consumer không chạy và đóng session có kết quả không chắc chắn.
        var connection = Connection(Command(result));
        var closed = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => SqlServerReadModelConsumerLock.AcquireAsync(connection.Object,
            () => { closed++; return Task.CompletedTask; }, default));

        Assert.Equal(1, closed);
        connection.Verify(x => x.CloseAsync(), Times.Once);
    }

    [Fact]
    public async Task AcquireLock_WhenBusy_ReturnsNoLeaseAndClosesNormally()
    {
        // Kiểm tra timeout0 do consumer khác giữ khóa chỉ bỏ qua lượt này và đóng connection đã mở.
        var connection = Connection(Command(-1));
        var closed = 0;

        var lease = await SqlServerReadModelConsumerLock.AcquireAsync(connection.Object,
            () => { closed++; return Task.CompletedTask; }, default);

        Assert.Null(lease);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task AcquireLock_WhenCancelledWhileWaiting_ClosesSessionAndPreservesCancellation()
    {
        // Kiểm tra lỗi hủy trong lúc lấy khóa vẫn đóng session và giữ đúng exception cancellation gốc.
        using var cancellation = new CancellationTokenSource();
        var original = new OperationCanceledException(cancellation.Token);
        var acquire = Command(0);
        acquire.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ThrowsAsync(original);
        var connection = Connection(acquire);
        var closed = false;

        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => SqlServerReadModelConsumerLock.AcquireAsync(connection.Object,
            () => { closed = true; return Task.CompletedTask; }, cancellation.Token));

        Assert.Same(original, error);
        Assert.True(closed);
        connection.Verify(x => x.CloseAsync(), Times.Once);
    }

    private static VeterinaryClinicReadDataContext SqlServerContext() => new(
        new DbContextOptionsBuilder<VeterinaryClinicReadDataContext>()
            .UseSqlServer("Server=unused;Database=ProjectionOffline;Integrated Security=true;TrustServerCertificate=true").Options);

    private static ReadModelProjectionState Snapshot(DbContext context, object entity)
    {
        var entry = context.Entry(entity);
        var values = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        var keys = entry.Metadata.FindPrimaryKey()!.Properties.ToDictionary(p => p.Name, p => values[p.Name]);
        return new ReadModelProjectionState(entry.Metadata.Name, keys, values);
    }

    private static Mock<DbCommand> Command(int result)
    {
        var command = new Mock<DbCommand>();
        command.SetupProperty(x => x.CommandText);
        command.SetupProperty(x => x.CommandTimeout);
        var parameters = new SqlCommand().Parameters;
        command.Protected().SetupGet<DbParameterCollection>("DbParameterCollection").Returns(parameters);
        command.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() => new SqlParameter());
        command.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync((object)result);
        return command;
    }

    private static Mock<DbConnection> Connection(params Mock<DbCommand>[] commands)
    {
        var connection = new Mock<DbConnection>();
        connection.SetupGet(x => x.State).Returns(ConnectionState.Open);
        connection.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);
        var queue = new Queue<Mock<DbCommand>>(commands);
        connection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(() => queue.Dequeue().Object);
        return connection;
    }
}
