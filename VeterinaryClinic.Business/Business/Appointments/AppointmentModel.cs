using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Base model for shared appointment input properties.
    /// </summary>
    public abstract record AppointmentBaseModel
    {
        [Required(ErrorMessage = "appointment.customer_id.required")]
        public int CustomerId { get; init; }

        [Required(ErrorMessage = "appointment.pet_id.required")]
        public int PetId { get; init; }

        [Required(ErrorMessage = "appointment.service_id.required")]
        public int ServiceId { get; init; }

        [Required(ErrorMessage = "appointment.appointment_date.required")]
        public DateTime AppointmentDate { get; init; }

        [Required(ErrorMessage = "appointment.start_time.required")]
        public DateTime StartTime { get; init; }

        public string? Note { get; init; }
        
        public DateTime? CreatedDate { get; init; }
    }

    /// <summary>
    /// DTO for displaying full appointment details.
    /// </summary>
    public record AppointmentModel : AppointmentBaseModel
    {
        public int DoctorId { get; init; }
        public int? BillId { get; init; }
        public DateTime EndTime { get; init; }

        // from TrackedChangeEntity
        public int Id { get; init; }

        // from BaseEntity
        public int Order { get; init; }
        public bool IsActive { get; init; } = true;

        // from BaseWorkflowEntity
        public string? AuthorId { get; init; }
        public Guid? ProcessId { get; init; }
        public string? State { get; init; }
        public string? StateName { get; init; }
        public bool IsFinalState { get; init; }

        // from VcAppointments
        public string? Code { get; init; }
        public string? CancelReason { get; init; }

        public string? CustomerName { get; init; }
        public string? CustomerPhone { get; init; }
        public string? PetName { get; init; }
        public string? ServiceName { get; init; }
        public string? ServicePrice { get; init; }
        public string? DoctorName { get; init; }

        public int MedicalRecordId { get; init; }
        
        public List<WorkflowCommandModel> Commands { get; init; } = new();
    }

    /// <summary>
    /// DTO for creating a new appointment. Doctor and end time are assigned by the system.
    /// </summary>
    public record CreateAppointmentModel : AppointmentBaseModel
    {
        public int? CreatedUserId { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerPhone { get; init; }
        public string? CustomerEmail { get; init; }
        public string? PetName { get; init; }
        public string? PetSpecies { get; init; }
        public string? PetBreed { get; init; }
        public DateTime? PetBirthDate { get; init; }
        public bool? PetGender { get; set; }
        public double? PetWeight { get; init; }
    }

    /// <summary>
    /// DTO for processing appointment status transitions.
    /// </summary>
    public record ProcessAppointmentModel
    {
        [Required(ErrorMessage = "appointment.action.required")]
        public string Action { get; init; } = string.Empty;

        public string? CancelReason { get; init; }
    }

    /// <summary>
    /// DTO for updating an existing appointment.
    /// </summary>
    public record UpdateAppointmentModel : AppointmentBaseModel
    {
        [Required(ErrorMessage = "appointment.doctor_id.required")]
        public int DoctorId { get; init; }

        [Required(ErrorMessage = "appointment.end_time.required")]
        public DateTime EndTime { get; init; }

        [Required]
        public int Id { get; init; }

        public void UpdateEntity(VcAppointments entity)
        {
            entity.CustomerId = this.CustomerId;
            entity.PetId = this.PetId;
            entity.ServiceId = this.ServiceId;
            entity.DoctorId = this.DoctorId;
            entity.AppointmentDate = this.AppointmentDate;
            entity.StartTime = this.StartTime;
            entity.EndTime = this.EndTime;
            entity.Note = this.Note ?? string.Empty;
        }
    }

    public record AppoinntmentFilterModel : BaseQueryFilterModel
    {
        public int? ServiceId { get; init; }
        public int? DoctorId { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public string? State { get; init; }
    }
    
    // Added model to map from DataTable
    public record CandidateDoctorModel
    {
        [DataColumn("doctor_id")]
        public int DoctorId { get; set; }
        
        [DataColumn("doctor_name")]
        public string DoctorName { get; set; }
        
        [DataColumn("doctor_email")]
        public string DoctorEmail { get; set; }
        
        [DataColumn("doctor_phone")]
        public string DoctorPhone { get; set; }
    }
}