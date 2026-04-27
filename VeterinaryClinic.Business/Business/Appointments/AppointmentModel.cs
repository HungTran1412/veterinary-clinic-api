using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Base model for appointment properties that can be edited.
    /// </summary>
    public abstract record AppointmentEditableModel
    {
        [Required(ErrorMessage = "appointment.customer_id.required")]
        public int CustomerId { get; init; }

        [Required(ErrorMessage = "appointment.pet_id.required")]
        public int PetId { get; init; }

        [Required(ErrorMessage = "appointment.service_id.required")]
        public int SerivceId { get; init; }

        [Required(ErrorMessage = "appointment.doctor_id.required")]
        public int DoctorId { get; init; }

        [Required(ErrorMessage = "appointment.appointment_date.required")]
        public DateTime AppointmentDate { get; init; }

        [Required(ErrorMessage = "appointment.start_time.required")]
        public DateTime StartTime { get; init; }

        [Required(ErrorMessage = "appointment.end_time.required")]
        public DateTime EndTime { get; init; }

        public string? Note { get; init; }
    }

    /// <summary>
    /// DTO for displaying full appointment details.
    /// </summary>
    public record AppointmentModel : AppointmentEditableModel
    {
        // from TrackedChangeEntity
        public int Id { get; init; }
        public DateTime? CreatedDate { get; init; }
        public int? CreatedUserId { get; init; }
        public string? CreatedUserName { get; init; }
        public DateTime? ModifiedDate { get; init; }
        public int? ModifiedUserId { get; init; }
        public string? ModifiedUserName { get; init; }

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
    }

    /// <summary>
    /// DTO for creating a new appointment.
    /// </summary>
    public record CreateAppointmentModel : AppointmentEditableModel
    {
    }

    /// <summary>
    /// DTO for updating an existing appointment.
    /// </summary>
    public record UpdateAppointmentModel : AppointmentEditableModel
    {
        [Required]
        public int Id { get; init; }

        public void UpdateEntity(VcAppointments entity)
        {
            entity.CustomerId = this.CustomerId;
            entity.PetId = this.PetId;
            entity.SerivceId = this.SerivceId;
            entity.DoctorId = this.DoctorId;
            entity.AppointmentDate = this.AppointmentDate;
            entity.StartTime = this.StartTime;
            entity.EndTime = this.EndTime;
            entity.Note = this.Note ?? string.Empty;
        }
    }
}
