namespace VeterinaryClinic.Data.Synchronization;

public sealed class ReadModelOutboxMessage
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string KeyValuesJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
