using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Shared;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class OutboxCommitSignalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task OwnedCommit_NotifiesAfterBusinessAndOutboxPersisted(int overload)
    {
        // Kiểm tra đủ overload SaveChanges chỉ báo sau khi business và Outbox đã commit, kể cả accept=false.
        using var fixture = new SignalFixture();
        var entity = NewSpecialization();
        fixture.Context.Add(entity);

        var count = await Save(fixture.Context, overload);

        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Equal(1, count);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(1, fixture.BusinessRowsAtNotification);
        Assert.Equal(1, fixture.OutboxRowsAtNotification);
        Assert.Equal(overload >= 4 ? EntityState.Added : EntityState.Unchanged, fixture.Context.Entry(entity).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCommit_AfterMultipleSaves_NotifiesOnce(bool async)
    {
        // Kiểm tra nhiều lần save trong transaction caller chỉ phát một tín hiệu sau commit thực tế.
        using var fixture = new SignalFixture();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        fixture.Context.Add(NewSpecialization("Lần thứ nhất"));
        await Save(fixture.Context, async ? 2 : 0);
        fixture.Context.Add(NewSpecialization("Lần thứ hai"));
        await Save(fixture.Context, async ? 5 : 4);
        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);

        if (async) await transaction.CommitAsync();
        else transaction.Commit();

        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(2, fixture.BusinessRowsAtNotification);
        Assert.Equal(2, fixture.OutboxRowsAtNotification);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CallerRollbackOrDispose_DoesNotNotifyOrLeakToNextTransaction(int rollback)
    {
        // Kiểm tra rollback hoặc dispose không phát tín hiệu và marker cũ không ảnh hưởng transaction chỉ ack sau đó.
        using var fixture = new SignalFixture();
        fixture.Context.Add(NewSpecialization("Bản ghi đã commit"));
        await fixture.Context.SaveChangesAsync();
        fixture.Signal.Invocations.Clear();
        await using (var transaction = await fixture.Context.Database.BeginTransactionAsync())
        {
            fixture.Context.Add(NewSpecialization("Bản ghi bị rollback"));
            await fixture.Context.SaveChangesAsync();
            fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
            if (rollback == 0) transaction.Rollback();
            else if (rollback == 1) await transaction.RollbackAsync();
        }
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(1, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(1, await fixture.Observer.ReadModelOutboxMessages.CountAsync());

        await using (var next = await fixture.Context.Database.BeginTransactionAsync())
        {
            var message = await fixture.Context.ReadModelOutboxMessages.SingleAsync();
            message.ProcessedAtUtc = DateTime.UtcNow;
            await fixture.Context.SaveChangesAsync();
            await next.CommitAsync();
        }

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.NotNull((await fixture.Observer.ReadModelOutboxMessages.SingleAsync()).ProcessedAtUtc);
        Assert.Equal(1, await fixture.Observer.VcSpecializations.CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedBusinessOrOutboxSave_DoesNotNotifyAndAllowsRetry(bool async, bool outboxFailure)
    {
        // Kiểm tra lỗi business hoặc Outbox rollback toàn bộ, không báo và chỉ báo một lần khi retry thành công.
        using var fixture = new SignalFixture();
        var entity = NewSpecialization();
        fixture.Context.Add(entity);
        if (outboxFailure) await RejectOutbox(fixture.Context);
        else entity.Name = null!;

        if (outboxFailure) await Assert.ThrowsAsync<SqliteException>(() => Save(fixture.Context, async ? 2 : 0));
        else await Assert.ThrowsAsync<DbUpdateException>(() => Save(fixture.Context, async ? 2 : 0));

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.Equal(0, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
        if (outboxFailure) await fixture.Context.Database.ExecuteSqlRawAsync("DROP TRIGGER [reject_outbox]");
        else entity.Name = "Dữ liệu đã sửa";
        await Save(fixture.Context, async ? 2 : 0);
        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(1, fixture.BusinessRowsAtNotification);
        Assert.Equal(1, fixture.OutboxRowsAtNotification);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedCommit_DoesNotNotifyAndAllowsRetry(bool async)
    {
        // Kiểm tra lỗi trước commit không phát tín hiệu, dữ liệu rollback và lần retry mới được đánh thức.
        var interceptor = new TransactionFailure();
        using var fixture = new SignalFixture(interceptor: interceptor);
        interceptor.FailCommit = true;
        fixture.Context.Add(NewSpecialization());

        await Assert.ThrowsAsync<IOException>(() => Save(fixture.Context, async ? 2 : 0));

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.Equal(0, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
        interceptor.FailCommit = false;
        await Save(fixture.Context, async ? 2 : 0);
        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(1, fixture.BusinessRowsAtNotification);
        Assert.Equal(1, fixture.OutboxRowsAtNotification);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSavepointRelease_DoesNotMarkCallerTransaction(bool async)
    {
        // Kiểm tra release savepoint thất bại không để marker báo sai khi caller commit transaction đã rollback phần save.
        var interceptor = new TransactionFailure();
        using var fixture = new SignalFixture(interceptor: interceptor);
        fixture.Context.Database.AutoSavepointsEnabled = false;
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        interceptor.FailRelease = true;
        fixture.Context.Add(NewSpecialization());

        await Assert.ThrowsAsync<IOException>(() => Save(fixture.Context, async ? 2 : 0));
        if (async) await transaction.CommitAsync();
        else transaction.Commit();

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.Equal(0, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedLaterSavepoint_PreservesEarlierCapturedNotification(bool async)
    {
        // Kiểm tra lỗi save sau chỉ rollback savepoint, vẫn báo khi caller commit Outbox của lần save trước.
        using var fixture = new SignalFixture();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        fixture.Context.Add(NewSpecialization("Được commit"));
        await Save(fixture.Context, async ? 2 : 0);
        await RejectOutbox(fixture.Context);
        fixture.Context.Add(NewSpecialization("Bị rollback"));

        await Assert.ThrowsAsync<SqliteException>(() => Save(fixture.Context, async ? 2 : 0));
        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        await transaction.CommitAsync();

        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(1, fixture.BusinessRowsAtNotification);
        Assert.Equal(1, fixture.OutboxRowsAtNotification);
        Assert.Equal("Được commit", (await fixture.Observer.VcSpecializations.SingleAsync()).Name);
    }

    [Fact]
    public async Task CancellationAfterBusinessSave_DoesNotNotifyAndAllowsRetry()
    {
        // Kiểm tra hủy sau business save vẫn rollback cả Outbox, không báo và không ảnh hưởng lần retry.
        using var cancellation = new CancellationTokenSource();
        using var fixture = new SignalFixture(interceptor: new CancelAfterSave(cancellation));
        fixture.Context.Add(NewSpecialization());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Context.SaveChangesAsync(cancellation.Token));

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.Equal(0, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(0, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
        await fixture.Context.SaveChangesAsync();
        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Null(fixture.ObservationError);
        Assert.Equal(1, fixture.BusinessRowsAtNotification);
        Assert.Equal(1, fixture.OutboxRowsAtNotification);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcknowledgementAndNoChanges_DoNotNotify(bool async)
    {
        // Kiểm tra ack Outbox và save không đổi dữ liệu không tạo tín hiệu đánh thức vòng lặp.
        using var fixture = new SignalFixture();
        fixture.Context.Add(NewSpecialization());
        await Save(fixture.Context, async ? 2 : 0);
        fixture.Signal.Invocations.Clear();
        var message = await fixture.Context.ReadModelOutboxMessages.SingleAsync();
        message.ProcessedAtUtc = DateTime.UtcNow;

        Assert.Equal(1, await Save(fixture.Context, async ? 2 : 0));
        Assert.Equal(0, await Save(fixture.Context, async ? 2 : 0));

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.NotNull((await fixture.Observer.ReadModelOutboxMessages.SingleAsync()).ProcessedAtUtc);
        Assert.Equal(1, await fixture.Observer.VcSpecializations.CountAsync());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task DisabledCaptureOrMissingSignal_PreservesSaveBehavior(bool enabled, bool injectSignal)
    {
        // Kiểm tra disabled không báo hay capture, còn thiếu signal vẫn lưu Outbox để polling xử lý.
        using var fixture = new SignalFixture(enabled, injectSignal);
        fixture.Context.Add(NewSpecialization());

        Assert.Equal(1, await fixture.Context.SaveChangesAsync());

        fixture.Signal.Verify(signal => signal.Notify(), Times.Never);
        Assert.Equal(1, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(enabled ? 1 : 0, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrowingNotifier_DoesNotFailCommittedSaveOrCallerCommit(bool async, bool external)
    {
        // Kiểm tra notifier lỗi sau commit không khiến SaveChanges hoặc caller commit báo thất bại dù dữ liệu đã lưu.
        using var fixture = new SignalFixture();
        fixture.Signal.Setup(signal => signal.Notify()).Throws(new IOException("signal unavailable"));
        await using var transaction = external ? await fixture.Context.Database.BeginTransactionAsync() : null;
        fixture.Context.Add(NewSpecialization());

        Assert.Equal(1, await Save(fixture.Context, async ? 2 : 0));
        if (transaction != null)
        {
            if (async) await transaction.CommitAsync();
            else transaction.Commit();
        }

        fixture.Signal.Verify(signal => signal.Notify(), Times.Once);
        Assert.Equal(1, await fixture.Observer.VcSpecializations.CountAsync());
        Assert.Equal(1, await fixture.Observer.ReadModelOutboxMessages.CountAsync());
    }

    private static Task<int> RejectOutbox(DbContext context) => context.Database.ExecuteSqlRawAsync(
        "CREATE TRIGGER [reject_outbox] BEFORE INSERT ON [vcReadModelOutbox] BEGIN SELECT RAISE(ABORT, 'outbox unavailable'); END");

    private static Task<int> Save(VeterinaryClinicDataContext context, int overload) => overload switch
    {
        0 => Task.FromResult(context.SaveChanges()),
        1 => Task.FromResult(context.SaveChanges(true)),
        2 => context.SaveChangesAsync(CancellationToken.None),
        3 => context.SaveChangesAsync(true, CancellationToken.None),
        4 => Task.FromResult(context.SaveChanges(false)),
        _ => context.SaveChangesAsync(false, CancellationToken.None)
    };

    private static VcSpecializations NewSpecialization(string name = "Khám tổng quát") =>
        new() { Code = Guid.NewGuid().ToString("N"), Name = name };

    private sealed class SignalFixture : IDisposable
    {
        private readonly SqliteConnection _writeConnection;
        private readonly SqliteConnection _observerConnection;
        public VeterinaryClinicDataContext Context { get; }
        public VeterinaryClinicDataContext Observer { get; }
        public Mock<IReadModelSynchronizationSignal> Signal { get; } = new();
        public int BusinessRowsAtNotification { get; private set; } = -1;
        public int OutboxRowsAtNotification { get; private set; } = -1;
        public Exception? ObservationError { get; private set; }

        public SignalFixture(bool enabled = true, bool injectSignal = true, IInterceptor? interceptor = null)
        {
            var connectionString = $"Data Source=outbox-signal-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False";
            _writeConnection = new SqliteConnection(connectionString);
            _observerConnection = new SqliteConnection(connectionString);
            _writeConnection.Open();
            _observerConnection.Open();
            var builder = new DbContextOptionsBuilder<VeterinaryClinicDataContext>().UseSqlite(_writeConnection);
            if (interceptor != null) builder.AddInterceptors(interceptor);
            var observerOptions = new DbContextOptionsBuilder<VeterinaryClinicDataContext>().UseSqlite(_observerConnection).Options;
            var accessor = Mock.Of<IContextAccessor>(value => value.UserId == 33 && value.UserName == "Người kiểm thử");
            Context = new VeterinaryClinicDataContext(builder.Options, () => accessor,
                Options.Create(new ReadModelSynchronizationOptions { Enabled = enabled }), injectSignal ? Signal.Object : null);
            Observer = new VeterinaryClinicDataContext(observerOptions, () => accessor);
            Context.Database.EnsureCreated();
            Signal.Setup(signal => signal.Notify()).Callback(() =>
            {
                try
                {
                    BusinessRowsAtNotification = Observer.VcSpecializations.Count();
                    OutboxRowsAtNotification = Observer.ReadModelOutboxMessages.Count();
                }
                catch (Exception error)
                {
                    ObservationError = error;
                }
            });
        }

        public void Dispose()
        {
            Observer.Dispose();
            Context.Dispose();
            _observerConnection.Dispose();
            _writeConnection.Dispose();
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

    private sealed class TransactionFailure : DbTransactionInterceptor
    {
        public bool FailCommit { get; set; }
        public bool FailRelease { get; set; }

        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result) => FailCommit ? throw new IOException("commit unavailable") : result;

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(TransactionCommitting(transaction, eventData, result));

        public override InterceptionResult ReleasingSavepoint(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result) => FailRelease ? throw new IOException("savepoint release unavailable") : result;

        public override ValueTask<InterceptionResult> ReleasingSavepointAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ReleasingSavepoint(transaction, eventData, result));
    }
}
