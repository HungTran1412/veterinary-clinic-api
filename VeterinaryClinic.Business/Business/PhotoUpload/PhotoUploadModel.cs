using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VeterinaryClinic.Business
{
    public record PhotoUploadBaseModel
    {
        [Required(ErrorMessage = "photo_upload.id.required")]
        public int Id { init; get; }
        
        public string? ImageUrl { init; get; }

        [JsonIgnore]
        public IFormFile? File { init; get; }
    }

    public record UploadPetPhotoModel : PhotoUploadBaseModel
    {
        
    }

    public record UploadServicePhotoModel : PhotoUploadBaseModel
    {
        
    }

    public record UploadUserPhotoModel
    {
        [Required(ErrorMessage = "photo_upload.id.required")]
        public int Id { get; init; }
        
        public string? AvatarUrl { init; get; }

        [JsonIgnore]
        public IFormFile? File { init; get; }
    }
}
