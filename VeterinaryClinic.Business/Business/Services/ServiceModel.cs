using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class ServiceBaseModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "service.code.required")]
        public string Code { get; set; }
        
        [Required(ErrorMessage = "service.name.required")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "service.price.required")]
        public decimal Price { get; set; }
        
        [Required(ErrorMessage = "service.duration_minutes.required")]
        public int DurationMinutes { get; set; }
        
        [Required(ErrorMessage = "service.specialization_id.required")]
        public int SpecializationId { get; set; }
        
        public string ImageUrl { get; set; }
        
        [Required(ErrorMessage = "service.is_available.required")]
        public bool IsAvailable { get; set; } = true;
        
        [MaxLength(1000)]
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;

        public int Order { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    
    public class ServiceModel : ServiceBaseModel
    {
    
    
    }

    public class CreateServiceModel : ServiceModel
    {
        public int? CreatedUserId { get; set; }
    }

    public class UpdateServiceModel : ServiceModel
    {
        public int? ModifiedUserId { get; set; }

        public void UpdateEntity(VcServices entity)
        {
            entity.Name = this.Name;
            entity.Price = this.Price;
            entity.DurationMinutes = this.DurationMinutes;
            entity.SpecializationId = this.SpecializationId;
            entity.ImageUrl = this.ImageUrl;
            entity.IsAvailable = this.IsAvailable;
            entity.Description = this.Description;
            entity.IsActive = this.IsActive;
            entity.Order = this.Order;
            entity.ModifiedUserId = entity.ModifiedUserId;
        }
    }

    public class ServiceSelectItemModel : SelectItemModel
    {
        
    }

    public class ServiceFilterModel : BaseQueryFilterModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public int? DurationMinutes { get; set; }
        public int? SpecializationId { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
