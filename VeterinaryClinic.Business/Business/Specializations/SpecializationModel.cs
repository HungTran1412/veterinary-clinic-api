using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class SpecializationBaseModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "specialization.code.required")]
        public String Code { get; set; }
        
        [Required(ErrorMessage = "specialization.name.required")]
        public string Name { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;

        public int Order { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    
    public class SpecializationModel : SpecializationBaseModel
    {
    
    }

    public class CreateSpecializationModel : SpecializationModel
    {
        public int? CreatedUserId { get; set; }
    }

    public class UpdateSpecializationModel : SpecializationModel
    {
        public int? ModifiedUserId { get; set; }

        public void UpdateEntity(VcSpecializations entity)
        {
            entity.Code = this.Code;
            entity.Name = this.Name;
            entity.Description = this.Description;
            entity.IsActive = this.IsActive;
            entity.Order = this.Order;
            entity.ModifiedUserId = entity.ModifiedUserId;
        }
    }

    public class SpecializationSelectItemModel : SelectItemModel
    {
        
    }

    public class SpecializationFilterModel : BaseQueryFilterModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
