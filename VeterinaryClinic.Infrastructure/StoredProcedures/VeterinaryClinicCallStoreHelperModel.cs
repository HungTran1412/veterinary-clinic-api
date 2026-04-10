using System.Data;

namespace VeterinaryClinic.Infrastructure;

public record VeterinaryClinicCallStoreHelperModel
{
    public DataTable Data { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; }
}