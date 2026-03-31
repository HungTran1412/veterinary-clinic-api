using Microsoft.AspNetCore.Http;

namespace VeterinaryClinic.Business
{
    public interface ICloudinaryService
    {
        Task<string> AddPhotoAsync(IFormFile file);
        Task<string> DeletePhotoAsync(string publicId);
    }    
}

