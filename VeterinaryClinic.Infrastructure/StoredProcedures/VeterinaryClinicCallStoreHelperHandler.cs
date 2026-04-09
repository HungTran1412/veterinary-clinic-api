using Microsoft.Extensions.Configuration;

namespace VeterinaryClinic.Infrastructure;

public class VeterinaryClinicCallStoreHelperHandler : IVeterinaryClinicCallStoreHelper
{
    private readonly IConfiguration _config;
    
    public VeterinaryClinicCallStoreHelperHandler(IConfiguration config)
    {
        _config = config;
    }

    private string DatabaseConnectionString()
    {
        return _config.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
}