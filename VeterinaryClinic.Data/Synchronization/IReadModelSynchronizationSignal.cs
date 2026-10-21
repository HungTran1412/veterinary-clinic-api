namespace VeterinaryClinic.Data.Synchronization;

public interface IReadModelSynchronizationSignal
{
    void Notify();
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
