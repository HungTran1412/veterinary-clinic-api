using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public abstract record WorkScheduleRegistrationBaseModel
    {
        public int Id { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.code.required")]
        public string Code { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.user-id.required")]
        public int UserId { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.shift-template-id.required")]
        public int ShiftTemplateId { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.work-date.required")]
        public DateTime WorkDate { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.status.required")]
        public string Status { get; init; }

        [Required(ErrorMessage = "work-schedule-registration.register-date.required")]
        public DateTime RegisterDate { get; init; }

        public string Note { get; init; }

        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }

    public record WorkScheduleRegistrationModel : WorkScheduleRegistrationBaseModel
    {
        public string? UserCode { get; init; }
        public string? FullName { get; init; }
        public string? Role { get; init; }
        public string? ShiftName { get; init; }
        public TimeOnly? ShiftStartTime { get; init; }
        public TimeOnly? ShiftEndTime { get; init; }
    }

    public record CreateWorkScheduleRegistrationModel : WorkScheduleRegistrationModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record ProcessWorkScheduleRegistrationModel
    {
        [Required(ErrorMessage = "work-schedule-registration.status.required")]
        public string Status { get; init; } = string.Empty;

        public string? Note { get; init; }
    }
    
    public record ProcessManyWorkScheduleRegistrationModel
    {
        [Required(ErrorMessage = "work-schedule-registration.ids.required")]
        [MinLength(1, ErrorMessage = "work-schedule-registration.ids.min_length")]
        public List<int> RegistrationIds { get; init; } = new List<int>();

        [Required(ErrorMessage = "work-schedule-registration.status.required")]
        public string Status { get; init; } = string.Empty;

        public string? Note { get; init; }
    }

    public record WorkScheduleRegistrationFilterModel : BaseQueryFilterModel
    {
        public string? Status { get; init; }
        public int? ShiftTemplateId { get; init; }
        public DateTime? FromWorkDate { get; init; }
        public DateTime? ToWorkDate { get; init; }
    }

    public record UpdateWorkScheduleRegistrationModel : WorkScheduleRegistrationBaseModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcWorkScheduleRegistrations entity)
        {
            entity.UserId = UserId;
            entity.ShiftTemplateId = ShiftTemplateId;
            entity.WorkDate = WorkDate;
            entity.Note = Note ?? string.Empty;
        }
    }
}
