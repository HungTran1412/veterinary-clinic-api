namespace VeterinaryClinic.Shared.ContextAccessor
{
    public interface IContextAccessor
    {
        string CorrelationId { get; }
        string TraceId { get; }
        int? UserId { get; }
        string UserName { get; }
        string Role { get; }
        string Language { get; }
    }
}