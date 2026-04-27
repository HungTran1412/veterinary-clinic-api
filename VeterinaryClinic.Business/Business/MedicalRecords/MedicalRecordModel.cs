using System;
using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public abstract record MedicalRecordBaseModel
    {
        public int Id { get; init; }

        [Required(ErrorMessage = "medical_record.appointment_id.required")]
        public int AppointmentId { get; init; }

        [Required(ErrorMessage = "medical_record.doctor_id.required")]
        public int DoctorId { get; init; }

        public string Code { get; init; }

        public string? Symptoms { get; init; }

        public string? Diagnosis { get; init; }

        public string? TreatmentPlan { get; init; }

        public string? Prescription { get; init; }

        public string? DoctorNote { get; init; }

        public DateTime? CompletedDate { get; init; }
    }

    public record MedicalRecordModel : MedicalRecordBaseModel
    {
    }

    public record CreateMedicalRecordModel : MedicalRecordModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateMedicalRecordModel : MedicalRecordModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcMedicalRecords entity)
        {
            entity.Diagnosis = this.Diagnosis;
            entity.Symptoms = this.Symptoms;
            entity.TreatmentPlan = this.TreatmentPlan;
            entity.Prescription = this.Prescription;
            entity.DoctorNote = this.DoctorNote;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record MedicalRecordFilterModel : BaseQueryFilterModel
    {
        
    }
}
