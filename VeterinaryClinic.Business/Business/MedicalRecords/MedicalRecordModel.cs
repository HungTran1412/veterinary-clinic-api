using System;
using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

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

    public record MedicalInfoModel
    {
        #region MyRegion

        [DataColumn("medical_record_id")] 
        public int MedicalRecordId { get; set; }

        [DataColumn("medical_record_code")] 
        public string? MedicalRecordCode { get; set; }

        [DataColumn("symptoms")] 
        public string? Symptoms { get; set; }

        [DataColumn("diagnosis")] 
        public string? Diagnosis { get; set; }

        [DataColumn("treatment_plan")] 
        public string? TreatmentPlan { get; set; }

        [DataColumn("prescription")] 
        public string? Prescription { get; set; }

        [DataColumn("doctor_note")] 
        public string? DoctorNote { get; set; }

        [DataColumn("completed_date")] 
        public DateTime? CompletedDate { get; set; }

        #endregion

        #region Appointment

        [DataColumn("appointment_id")] 
        public int AppointmentId { get; set; }

        [DataColumn("appointment_code")] 
        public string AppointmentCode { get; set; }
        
        [DataColumn("appointment_date")] 
        public DateTime? AppointmentDate { get; set; }

        [DataColumn("start_time")] 
        public DateTime? StartTime { get; set; }

        [DataColumn("end_time")] 
        public DateTime? EndTime { get; set; }

        [DataColumn("appointment_note")] 
        public string? AppointmentNote { get; set; }

        #endregion

        #region Pet

        [DataColumn("pet_id")] 
        public int PetId { get; set; }

        [DataColumn("pet_name")] 
        public string? PetName { get; set; }

        [DataColumn("species")] 
        public string? Species { get; set; }

        [DataColumn("breed")] 
        public string? Breed { get; set; }

        [DataColumn("gender")] 
        public bool? Gender { get; set; }

        [DataColumn("is_neutered")] 
        public bool? IsNeutered { get; set; }

        [DataColumn("birth_date")] 
        public DateTime? BirthDate { get; set; }

        [DataColumn("weight")] 
        public double? Weight { get; set; }

        [DataColumn("color")] 
        public string? Color { get; set; }

        [DataColumn("image_url")] 
        public string? ImageUrl { get; set; }
        

        #endregion

        #region Doctor

        [DataColumn("doctor_id")] 
        public int DoctorId { get; set; }

        [DataColumn("doctor_name")] 
        public string? DoctorName { get; set; }

        [DataColumn("doctor_phone")] 
        public string? DoctorPhone { get; set; }

        [DataColumn("doctor_email")] 
        public string? DoctorEmail { get; set; }

        #endregion
    }

    public record MedicalRecordFilterModel : BaseQueryFilterModel
    {
    }
}