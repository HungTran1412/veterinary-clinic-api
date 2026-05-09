using System.Data;

namespace VeterinaryClinic.Business;

public interface IVeterinaryClinicCallStoreHelper
{
    /// <summary>
    /// Lấy thông tin lịch hẹn
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    DataTable CallStoreGetMedicalRecordByIdAsync(int id);
}