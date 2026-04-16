using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business
{
    public abstract record WorkScheduleBaseModel
    {
        public int Id { get; init; }
        
        public string? Code { get; init; }
        
        [Required(ErrorMessage = "work_schedule.user_id.required")]
        public int UserId { get; init; }
        
        [Required(ErrorMessage = "work_schedule.work_date.required")]
        public DateTime WorkDate { get; init; }

        [Required(ErrorMessage = "work_schedule.start_time.required")]
        public DateTime StartTime { get; init; }

        [Required(ErrorMessage = "work_schedule.end_time.required")]
        public DateTime EndTime { get; init; }
        
        [Required(ErrorMessage = "work_schedule.shift_name.required")]
        public string ShiftName { get; init; }
        
        // Made Note nullable
        public string? Note { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }
    
    public record WorkScheduleModel : WorkScheduleBaseModel
    {
        public string FullName { get; init; }
        public string Role { get; init; }
    }

    public record CreateWorkScheduleModel : WorkScheduleBaseModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateWorkScheduleModel : WorkScheduleBaseModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcWorkSchedules entity)
        {
            entity.UserId = this.UserId;
            entity.WorkDate = this.WorkDate;
            entity.StartTime = this.StartTime;
            entity.EndTime = this.EndTime;
            entity.ShiftName = this.ShiftName;
            entity.Note = this.Note;
        }
    }

    public record WorkScheduleFilterModel
    {
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public string TextSearch { get; init; }
        public string Role { get; init; }
    }
}
