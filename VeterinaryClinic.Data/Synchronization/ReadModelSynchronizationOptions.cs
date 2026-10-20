namespace VeterinaryClinic.Data.Synchronization;

public sealed class ReadModelSynchronizationOptions
{
    public const string SectionName = "ReadModelSynchronization";

    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
}
