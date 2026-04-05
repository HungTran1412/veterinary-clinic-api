using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public record ServiceBaseModel
    {
        public int Id { get; init; }
        
        [Required(ErrorMessage = "service.code.required")]
        public string Code { get; init; }
        
        [Required(ErrorMessage = "service.name.required")]
        public string Name { get; init; }
        
        [Required(ErrorMessage = "service.price.required")]
        public decimal Price { get; init; }
        
        [Required(ErrorMessage = "service.duration_minutes.required")]
        public int DurationMinutes { get; init; }
        
        [Required(ErrorMessage = "service.specialization_id.required")]
        public int SpecializationId { get; init; }
        
        public string? SpecializationName { get; init; }
        
        public string? ImageUrl { get; init; }
        
        [Required(ErrorMessage = "service.is_available.required")]
        public bool IsAvailable { get; init; } = true;

        [MaxLength(1000)] public string? Description { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }
    
    public record ServiceModel : ServiceBaseModel
    {
    
    
    }

    public record InfoServiceModel : ServiceModel
    {
        public string SpecializationName { get; init; }
    }
    
    public record CreateServiceModel : ServiceModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateServiceModel : ServiceModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcServices entity)
        {
            entity.Name = this.Name;
            entity.Price = this.Price;
            entity.DurationMinutes = this.DurationMinutes;
            entity.SpecializationId = this.SpecializationId;
            entity.ImageUrl = string.IsNullOrEmpty(this.ImageUrl) ? "" : this.ImageUrl; // Xử lý null an toàn
            entity.IsAvailable = this.IsAvailable;
            entity.Description = string.IsNullOrEmpty(this.Description) ? "" : this.Description; // Xử lý null an toàn
            entity.Order = this.Order;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record ServiceSelectItemModel : SelectItemModel
    {
        
    }

    public record ServiceFilterModel : BaseQueryFilterModel
    {
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public int? MinDurationMinutes { get; init; }
        public int? MaxDurationMinutes { get; init; }
        public int? SpecializationId { get; init; }
        public bool? IsAvailable { get; init; }
    }
}
