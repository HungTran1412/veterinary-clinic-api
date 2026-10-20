using System.Text.Json;
using System.Transactions;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Moq;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Shared;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class OutboxCaptureTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task EnabledSave_PersistsGeneratedKeyWithoutBusinessPayload(int overload)
    {
        // Kiểm tra lưu business và Outbox cùng khóa identity thật, không sao chép nội dung nghiệp vụ.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization();
        fixture.Context.Add(entity);

        var count = await Save(fixture.Context, overload);

        Assert.Equal(1, count);
        Assert.True(entity.Id > 0);
        var message = Assert.Single(await fixture.Context.ReadModelOutboxMessages.ToListAsync());
        Assert.Equal(typeof(VcSpecializations).FullName, message.EntityType);
        using var key = JsonDocument.Parse(message.KeyValuesJson);
        Assert.Single(key.RootElement.EnumerateObject());
        Assert.Equal(entity.Id, key.RootElement.GetProperty("Id").GetInt32());
        Assert.DoesNotContain(entity.Name, message.KeyValuesJson);
        Assert.Equal(33, entity.CreatedUserId);
        Assert.Equal("Người kiểm thử", entity.CreatedUserName);
        Assert.Equal(33, entity.ModifiedUserId);
        Assert.NotNull(entity.ModifiedDate);
        Assert.Null(message.ProcessedAtUtc);
        Assert.True(message.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAndHardDelete_CaptureTheSamePersistedPrimaryKey(bool async)
    {
        // Kiểm tra cập nhật và xóa cứng đều tạo Outbox với khóa bản ghi gốc.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization();
        fixture.Context.Add(entity);
        await Save(fixture.Context, async ? 2 : 0);
        var key = entity.Id;
        var createdDate = entity.CreatedDate;

        entity.Name = "Tên đã cập nhật";
        Assert.Equal(1, await Save(fixture.Context, async ? 2 : 0));
        Assert.Equal(createdDate, entity.CreatedDate);
        Assert.Equal(33, entity.ModifiedUserId);
        fixture.Context.Remove(entity);
        Assert.Equal(1, await Save(fixture.Context, async ? 2 : 0));

        Assert.Empty(await fixture.Context.VcSpecializations.ToListAsync());
        var messages = await fixture.Context.ReadModelOutboxMessages.ToListAsync();
        Assert.Equal(3, messages.Count);
        Assert.All(messages, message => Assert.Equal(key,
            JsonDocument.Parse(message.KeyValuesJson).RootElement.GetProperty("Id").GetInt32()));
        Assert.Equal(EntityState.Detached, fixture.Context.Entry(entity).State);
    }

    [Fact]
    public async Task CompositeKey_InsertAndDeletePreserveEveryKeyPart()
    {
        // Kiểm tra entity khóa ghép đồng bộ đủ hai thành phần khi thêm và xóa.
        using var fixture = new CaptureFixture();
        var entity = new VcDoctorSpecializations { DoctorId = 7, SpecializationId = 9 };
        fixture.Context.Add(entity);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Remove(entity);
        await fixture.Context.SaveChangesAsync();

        Assert.Empty(await fixture.Context.VcDoctorSpecializations.ToListAsync());
        var messages = await fixture.Context.ReadModelOutboxMessages.ToListAsync();
        Assert.Equal(2, messages.Count);
        foreach (var message in messages)
        {
            using var keys = JsonDocument.Parse(message.KeyValuesJson);
            Assert.Equal(2, keys.RootElement.EnumerateObject().Count());
            Assert.Equal(7, keys.RootElement.GetProperty("DoctorId").GetInt32());
            Assert.Equal(9, keys.RootElement.GetProperty("SpecializationId").GetInt32());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveWithoutAcceptingChanges_PreservesStateUntilCallerAccepts(bool async)
    {
        // Kiểm tra SaveChanges(false) giữ trạng thái Added và cho phép caller tự AcceptAllChanges.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization();
        var entry = fixture.Context.Add(entity);

        var count = async ? await fixture.Context.SaveChangesAsync(false) : fixture.Context.SaveChanges(false);

        Assert.Equal(1, count);
        Assert.Equal(EntityState.Added, entry.State);
        var generatedKey = entry.Property(value => value.Id).CurrentValue;
        Assert.True(generatedKey > 0);
        Assert.Equal(generatedKey, JsonDocument.Parse(Assert.Single(await fixture.Context.ReadModelOutboxMessages.ToListAsync())
            .KeyValuesJson).RootElement.GetProperty("Id").GetInt32());
        fixture.Context.ChangeTracker.AcceptAllChanges();
        Assert.Equal(generatedKey, entity.Id);
        Assert.Equal(EntityState.Unchanged, entry.State);
        entity.Name = "Đã chấp nhận và sửa tiếp";
        await Save(fixture.Context, async ? 2 : 0);
        Assert.Equal(1, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(2, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task SaveWithoutAcceptingChanges_PreservesModifiedAndDeletedStates()
    {
        // Kiểm tra SaveChanges(false) giữ cả trạng thái Modified, giá trị gốc và Deleted theo hợp đồng EF.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization("Tên gốc");
        var entry = fixture.Context.Add(entity);
        await fixture.Context.SaveChangesAsync();

        entity.Name = "Tên mới";
        await fixture.Context.SaveChangesAsync(false);

        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal("Tên gốc", entry.OriginalValues[nameof(VcSpecializations.Name)]);
        fixture.Context.ChangeTracker.AcceptAllChanges();
        fixture.Context.Remove(entity);
        await fixture.Context.SaveChangesAsync(false);
        Assert.Equal(EntityState.Deleted, entry.State);
        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(3, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        fixture.Context.ChangeTracker.AcceptAllChanges();
        Assert.Equal(EntityState.Detached, entry.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedUpdateOrDelete_RestoresOriginalStateForRetry(bool delete)
    {
        // Kiểm tra lỗi Outbox khi sửa hoặc xóa khôi phục dữ liệu và trạng thái để retry cùng context.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization("Tên gốc");
        fixture.Context.Add(entity);
        await fixture.Context.SaveChangesAsync();
        await RejectOutbox(fixture.Context);
        if (delete) fixture.Context.Remove(entity);
        else entity.Name = "Tên sửa";

        await Assert.ThrowsAsync<SqliteException>(() => fixture.Context.SaveChangesAsync());

        Assert.Equal(delete ? EntityState.Deleted : EntityState.Modified, fixture.Context.Entry(entity).State);
        Assert.Equal("Tên gốc", (await fixture.Context.VcSpecializations.AsNoTracking().SingleAsync()).Name);
        Assert.Equal(1, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        await fixture.Context.Database.ExecuteSqlRawAsync("DROP TRIGGER [reject_outbox]");
        await fixture.Context.SaveChangesAsync();
        if (delete) Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        else Assert.Equal("Tên sửa", (await fixture.Context.VcSpecializations.AsNoTracking().SingleAsync()).Name);
        Assert.Equal(2, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task UserChange_PayloadContainsOnlyIdAndNeverCredentials()
    {
        // Kiểm tra Outbox không lưu mật khẩu, token hoặc thông tin người dùng ngoài khóa chính.
        using var fixture = new CaptureFixture();
        var user = new VcUsers
        {
            Code = "USER_TEST", Username = "tester", Email = "tester@example.invalid", Password = "password-secret",
            FullName = "Người kiểm thử", PhoneNumber = "0123456789", AvatarUrl = "avatar", Role = "CUSTOMER",
            RefreshToken = "refresh-token-secret", VerificationToken = "verification-secret"
        };
        fixture.Context.Add(user);

        await fixture.Context.SaveChangesAsync();

        var message = Assert.Single(await fixture.Context.ReadModelOutboxMessages.ToListAsync());
        using var payload = JsonDocument.Parse(message.KeyValuesJson);
        Assert.Single(payload.RootElement.EnumerateObject());
        Assert.Equal(user.Id, payload.RootElement.GetProperty("Id").GetInt32());
        Assert.DoesNotContain("secret", message.KeyValuesJson);
        Assert.DoesNotContain("tester", message.KeyValuesJson);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisabledCapture_DoesNotRequireAnOutboxTable(bool async)
    {
        // Kiểm tra chế độ một DB vẫn lưu và audit khi chưa triển khai bảng Outbox.
        using var fixture = new CaptureFixture(enabled: false);
        await fixture.Context.Database.ExecuteSqlRawAsync("DROP TABLE [vcReadModelOutbox]");
        var entity = NewSpecialization();
        fixture.Context.Add(entity);

        var count = await Save(fixture.Context, async ? 2 : 0);

        Assert.Equal(1, count);
        Assert.True(entity.Id > 0);
        Assert.Equal(33, entity.CreatedUserId);
        Assert.Equal(1, await fixture.Context.VcSpecializations.CountAsync());
    }

    [Fact]
    public async Task AcknowledgingOutbox_DoesNotEmitAnotherMessage()
    {
        // Kiểm tra cập nhật trạng thái xử lý Outbox không phát sinh vòng lặp sự kiện.
        using var fixture = new CaptureFixture();
        fixture.Context.Add(NewSpecialization());
        await fixture.Context.SaveChangesAsync();
        var message = Assert.Single(await fixture.Context.ReadModelOutboxMessages.ToListAsync());

        message.ProcessedAtUtc = DateTime.UtcNow;
        await fixture.Context.SaveChangesAsync();
        Assert.Equal(0, await fixture.Context.SaveChangesAsync());

        Assert.Equal(1, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        Assert.NotNull((await fixture.Context.ReadModelOutboxMessages.SingleAsync()).ProcessedAtUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerRollback_RemovesBusinessAndOutboxFromMultipleSaves(bool async)
    {
        // Kiểm tra capture không tự commit transaction caller và rollback xóa toàn bộ các lần save.
        using var fixture = new CaptureFixture();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        fixture.Context.Add(NewSpecialization("Lần thứ nhất"));
        await Save(fixture.Context, async ? 2 : 0);
        fixture.Context.Add(NewSpecialization("Lần thứ hai"));
        await Save(fixture.Context, async ? 2 : 0);
        Assert.Equal(2, await fixture.Context.ReadModelOutboxMessages.CountAsync());

        await transaction.RollbackAsync();

        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxFailure_RollsBackGeneratedIdentityAndAllowsRetry(bool async)
    {
        // Kiểm tra lỗi Outbox sau insert business rollback cả hai và retry cùng context không tạo bản ghi thừa.
        using var fixture = new CaptureFixture();
        await RejectOutbox(fixture.Context);
        var entity = NewSpecialization();
        fixture.Context.Add(entity);

        await Assert.ThrowsAsync<SqliteException>(() => Save(fixture.Context, async ? 2 : 0));

        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        Assert.Equal(EntityState.Added, fixture.Context.Entry(entity).State);
        await fixture.Context.Database.ExecuteSqlRawAsync("DROP TRIGGER [reject_outbox]");
        await Save(fixture.Context, async ? 2 : 0);
        var persisted = await fixture.Context.VcSpecializations.AsNoTracking().SingleAsync();
        Assert.Equal(entity.Id, persisted.Id);
        Assert.Equal(entity.Id, JsonDocument.Parse((await fixture.Context.ReadModelOutboxMessages.SingleAsync())
            .KeyValuesJson).RootElement.GetProperty("Id").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerSavepointFailure_PreservesEarlierSaveAndCanCommitRetry(bool async)
    {
        // Kiểm tra savepoint chỉ rollback lần save lỗi, giữ lần trước và cho caller commit sau retry.
        using var fixture = new CaptureFixture();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        fixture.Context.Add(NewSpecialization("Đã lưu trước"));
        await Save(fixture.Context, async ? 2 : 0);
        await RejectOutbox(fixture.Context);
        fixture.Context.Add(NewSpecialization("Lưu sau và lỗi"));

        await Assert.ThrowsAsync<SqliteException>(() => Save(fixture.Context, async ? 2 : 0));

        Assert.Equal(1, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(1, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        await fixture.Context.Database.ExecuteSqlRawAsync("DROP TRIGGER [reject_outbox]");
        await Save(fixture.Context, async ? 2 : 0);
        await transaction.CommitAsync();
        Assert.Equal(2, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(2, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task BusinessConstraintFailure_DoesNotInsertOutboxAndCanBeCorrected()
    {
        // Kiểm tra lỗi ràng buộc business không ghi Outbox và vẫn retry được sau khi sửa dữ liệu.
        using var fixture = new CaptureFixture();
        var entity = NewSpecialization();
        entity.Name = null!;
        fixture.Context.Add(entity);

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());

        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        entity.Name = "Dữ liệu đã hợp lệ";
        await fixture.Context.SaveChangesAsync();
        Assert.Equal(1, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(1, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task CancellationAfterBusinessSave_RollsBackWithAnUncancelledToken()
    {
        // Kiểm tra token bị hủy sau business save vẫn rollback được và retry không mất dữ liệu hay nhân đôi.
        using var cancellation = new CancellationTokenSource();
        using var fixture = new CaptureFixture(interceptor: new CancelAfterSave(cancellation));
        var entity = NewSpecialization();
        fixture.Context.Add(entity);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Context.SaveChangesAsync(cancellation.Token));

        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        Assert.Equal(EntityState.Added, fixture.Context.Entry(entity).State);
        await fixture.Context.SaveChangesAsync();
        Assert.Equal(1, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(1, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task PreCancelledSave_DoesNotPersistAnything()
    {
        // Kiểm tra cancellation có sẵn được trả về trước khi lưu business hoặc Outbox.
        using var fixture = new CaptureFixture();
        fixture.Context.Add(NewSpecialization());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Context.SaveChangesAsync(new CancellationToken(true)));

        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task AmbientTransaction_IsRejectedBeforeBusinessWrites()
    {
        // Kiểm tra ambient transaction không được hỗ trợ bị từ chối trước mọi thay đổi dữ liệu.
        using var fixture = new CaptureFixture();
        fixture.Context.Add(NewSpecialization());

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
            Assert.Contains("ngầm định", error.Message);
        }

        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerTransactionWithoutSavepoints_IsRejectedBeforeBusinessWrites(bool async)
    {
        // Kiểm tra transaction không có savepoint bị từ chối trước ghi business, như cấu hình SQL Server bật MARS.
        using var fixture = new CaptureFixture(supportsSavepoints: false);
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        Assert.False(transaction.SupportsSavepoints);
        fixture.Context.Add(NewSpecialization());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(fixture.Context, async ? 2 : 0));

        Assert.Contains("savepoint", error.Message);
        Assert.Equal(0, await fixture.Context.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Context.ReadModelOutboxMessages.CountAsync());
        await transaction.RollbackAsync();
    }

    private static Task<int> Save(VeterinaryClinicDataContext context, int overload) => overload switch
    {
        0 => Task.FromResult(context.SaveChanges()),
        1 => Task.FromResult(context.SaveChanges(true)),
        2 => context.SaveChangesAsync(CancellationToken.None),
        _ => context.SaveChangesAsync(true, CancellationToken.None)
    };

    private static Task<int> RejectOutbox(DbContext context) => context.Database.ExecuteSqlRawAsync(
        "CREATE TRIGGER [reject_outbox] BEFORE INSERT ON [vcReadModelOutbox] BEGIN SELECT RAISE(ABORT, 'outbox unavailable'); END");

    private static VcSpecializations NewSpecialization(string name = "Khám tổng quát") =>
        new() { Code = Guid.NewGuid().ToString("N"), Name = name };

    private sealed class CaptureFixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public VeterinaryClinicDataContext Context { get; }

        public CaptureFixture(bool enabled = true, IInterceptor? interceptor = null, bool supportsSavepoints = true)
        {
            _connection.Open();
            var builder = new DbContextOptionsBuilder<VeterinaryClinicDataContext>().UseSqlite(_connection);
            if (interceptor != null) builder.AddInterceptors(interceptor);
            if (!supportsSavepoints) builder.ReplaceService<IRelationalTransactionFactory, NoSavepointTransactionFactory>();
            var options = builder.Options;
            var accessor = Mock.Of<IContextAccessor>(value => value.UserId == 33 && value.UserName == "Người kiểm thử");
            Context = new VeterinaryClinicDataContext(options, () => accessor,
                Options.Create(new ReadModelSynchronizationOptions { Enabled = enabled }));
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class CancelAfterSave(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        private bool _cancelNext = true;

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (_cancelNext)
            {
                _cancelNext = false;
                cancellation.Cancel();
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class NoSavepointTransactionFactory(RelationalTransactionFactoryDependencies dependencies)
        : RelationalTransactionFactory(dependencies)
    {
        public override RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction,
            Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned) =>
            new NoSavepointTransaction(connection, transaction, transactionId, logger, transactionOwned, Dependencies.SqlGenerationHelper);
    }

    private sealed class NoSavepointTransaction(IRelationalConnection connection, DbTransaction transaction,
        Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned,
        ISqlGenerationHelper sqlGenerationHelper)
        : RelationalTransaction(connection, transaction, transactionId, logger, transactionOwned, sqlGenerationHelper)
    {
        public override bool SupportsSavepoints => false;
    }
}
