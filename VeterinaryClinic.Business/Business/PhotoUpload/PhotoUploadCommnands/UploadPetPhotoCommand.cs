using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business;

public class UploadPetPhotoCommand : IRequest<UploadPetPhotoModel>
{
    public UploadPetPhotoModel Model { get; }

    /// <summary>
    /// Tải ảnh lên cho thú cưng
    /// </summary>
    /// <param name="model"></param>
    public UploadPetPhotoCommand(UploadPetPhotoModel model)
    {
        Model = model;
    }

    public class Handler : IRequestHandler<UploadPetPhotoCommand, UploadPetPhotoModel>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<UploadPetPhotoCommand> _localizer;

        public Handler(VeterinaryClinicDataContext dataContext, ICloudinaryService cloudinaryService, ICacheService cacheService, IStringLocalizer<UploadPetPhotoCommand> localizer)
        {
            _dataContext = dataContext;
            _cloudinaryService = cloudinaryService;
            _cacheService = cacheService;
            _localizer = localizer;
        }

        public async Task<UploadPetPhotoModel> Handle(UploadPetPhotoCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            Log.Information($"Upload Pet Photo for Pet:{model.Id}");

            if (model.File == null || model.File.Length == 0)
            {
                throw new ArgumentException(_localizer["photo_upload.file.required"]);
            }
            
            // Kiem tra xem thu cung co ton tai khong
            var data = await _dataContext.VcPets.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (data == null)
            {
                throw new ArgumentException(_localizer["data.not-found"]);
            }
            
            //upload photo to cloudinary
            var imageUrl = await _cloudinaryService.AddPhotoAsync(model.File);

            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new Exception("Failed to upload photo.");
            }
            
            //cap nhat anh vao database
            data.ImageUrl = imageUrl;
            _dataContext.VcPets.Update(data);
            await _dataContext.SaveChangesAsync(cancellationToken);
            
            //xoa cache
            _cacheService.Remove(PhotoUploadConstant.BuildCacheKey(data.Id.ToString()));
            _cacheService.Remove(PhotoUploadConstant.BuildCacheKey());
            
            Log.Information($"Successfully uploaded photo for PetId: {model.Id}. New ImageUrl: {imageUrl}");
            
            return model with { ImageUrl = imageUrl };
        }
    }
}