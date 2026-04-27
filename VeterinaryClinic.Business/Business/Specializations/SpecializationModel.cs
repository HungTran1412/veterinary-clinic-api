using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public abstract record SpecializationBaseModel
    {
        public int Id { get; init; }
        
        [Required(ErrorMessage = "specialization.code.required")]
        public String Code { get; init; }
        
        [Required(ErrorMessage = "specialization.name.required")]
        public string Name { get; init; }
        
        [MaxLength(1000)]
        public string? Description { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }
    
    public record SpecializationModel : SpecializationBaseModel
    {
    
    }

    public record CreateSpecializationModel : SpecializationModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateSpecializationModel : SpecializationModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcSpecializations entity)
        {
            entity.Name = this.Name;
            entity.Description = this.Description;
            entity.ModifiedUserId = entity.ModifiedUserId;
        }
    }

    public record SpecializationSelectItemModel : SelectItemModel
    {
        
    }

    public record SpecializationFilterModel : BaseQueryFilterModel
    {
        public string? Code { get; init; }
        public string? Name { get; init; }
    }
}
