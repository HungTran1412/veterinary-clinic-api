using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VeterinaryClinic.Business
{
    
    
    public abstract record PhotoUploadBaseModel
    {
        [Required(ErrorMessage = "photo_upload.id.required")]
        public int Id { init; get; }
        
        [JsonIgnore]
        public IFormFile? File { init; get; }
    }

    public record PhotoUploadModel : PhotoUploadBaseModel
    {
        public string? ImageUrl { init; get; }
    }

    public record UploadPetPhotoModel : PhotoUploadModel
    {
        
    }

    public record UploadServicePhotoModel : PhotoUploadModel
    {
        
    }

    public record UploadUserPhotoModel : PhotoUploadBaseModel
    {
        public string? AvatarUrl { init; get; }
    }
}
