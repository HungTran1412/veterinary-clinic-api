using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace VeterinaryClinic.Business;

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

    public DataTable CallStoreGetMedicalRecordByIdAsync(int id)
    {
        using var conn = new SqlConnection(DatabaseConnectionString());
        using var cmd = new SqlCommand("sp_GetMedicalRecordById", conn);

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@MedicalRecordId", id);
        
        var dt = new DataTable();

        try
        {
            conn.Open();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CallStore Error");
            throw;
        }

        return dt;
    }

    public DataTable CallStoreGetCandidateDoctorsAsync(
        int SpecializatoinId, 
        DateTime AppointmentDate,
        DateTime StartTime,
        DateTime EndTime)
    {
        using var conn = new SqlConnection(DatabaseConnectionString());
        using var cmd = new SqlCommand("sp_GetCandidateDoctors", conn);

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@SpecializationId", SpecializatoinId);
        cmd.Parameters.AddWithValue("@AppointmentDate", AppointmentDate);
        cmd.Parameters.AddWithValue("@StartTime", StartTime);
        cmd.Parameters.AddWithValue("@EndTime", EndTime);

        var dt = new DataTable();

        try
        {
            conn.Open();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CallStore Error");
            throw;
        }

        return dt;
    }

    public DataTable CallStoreDashboardRevenueOverviewAsync(int Month)
    {
        using var conn = new SqlConnection(DatabaseConnectionString());
        using var cmd = new SqlCommand("sp_DashboardRevenueOverview", conn);

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Month", Month);

        var dt = new DataTable();

        try
        {
            conn.Open();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CallStore Error");
            throw;
        }

        return dt;
    }
}