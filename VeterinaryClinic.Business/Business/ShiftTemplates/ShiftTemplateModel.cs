using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public record ShiftTemplateBaseModel
    {
        public int Id { get; init; }
        [Required(ErrorMessage = "shift_template.code.required")]
        public string Code { get; init; }
    
        [Required(ErrorMessage = "shift_template.shift_name.required")]    
        public string ShiftName { get; init; }
    
        [Required(ErrorMessage = "shift_template.start_time.required")]    
        public TimeOnly StartTime { get; init; }
    
        [Required(ErrorMessage = "shift_template.end_time.required")]    
        public TimeOnly EndTime { get; init; }

        public int MaxEmployee { get; init; }
    }

    public record ShiftTemplateModel : ShiftTemplateBaseModel
    {
    
    }

    public record CreateShiftTemplateModel : ShiftTemplateModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateShiftTemplateModel : ShiftTemplateModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcShiftTemplates entity)
        {
            entity.ShiftName = this.ShiftName;
            entity.StartTime = this.StartTime;
            entity.EndTime = this.EndTime;
            entity.MaxEmployee = this.MaxEmployee;
        }
    }

    public record ShiftTemplateSelectItemModel : SelectItemModel
    {
        
    }

    public record ShiftTemplateFilterModel : BaseQueryFilterModel
    {
    }
}

