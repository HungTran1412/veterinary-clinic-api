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

    /// <summary>
    /// lay thong tin bac si di lam trong ca day
    /// </summary>
    /// <param name="SpecializatoinId"></param>
    /// <param name="AppointmentDate"></param>
    /// <param name="StartTime"></param>
    /// <param name="EndTime"></param>
    /// <returns></returns>
    DataTable CallStoreGetCandidateDoctorsAsync(int SpecializatoinId, DateTime AppointmentDate, DateTime StartTime,
        DateTime EndTime);
}