using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace VeterinaryClinic.Data.Synchronization;

internal sealed class ReadModelOutboxTransactionInterceptor(IReadModelSynchronizationSignal signal) : DbTransactionInterceptor
{
    private DbTransaction? _pendingTransaction;

    public void MarkCaptured(DbTransaction transaction) => _pendingTransaction = transaction;

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        NotifyCommitted(transaction);

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        NotifyCommitted(transaction);
        return Task.CompletedTask;
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) =>
        ClearPending(transaction);

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ClearPending(transaction);
        return Task.CompletedTask;
    }

    private void ClearPending(DbTransaction transaction)
    {
        if (ReferenceEquals(_pendingTransaction, transaction))
            _pendingTransaction = null;
    }

    private void NotifyCommitted(DbTransaction transaction)
    {
        if (!ReferenceEquals(_pendingTransaction, transaction))
            return;

        _pendingTransaction = null;
        try
        {
            signal.Notify();
        }
        catch (Exception)
        {
            // Outbox và polling vẫn bảo đảm xử lý nếu tín hiệu đánh thức bị lỗi sau commit.
        }
    }
}
