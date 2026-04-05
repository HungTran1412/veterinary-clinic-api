using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UploadServicePhotoCommand : IRequest<UploadServicePhotoModel>
    {
        public UploadServicePhotoModel Model { get; }

        /// <summary>
        /// Tải ảnh lên cho dịch vụ
        /// </summary>
        /// <param name="model"></param>
        public UploadServicePhotoCommand(UploadServicePhotoModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<UploadServicePhotoCommand, UploadServicePhotoModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICloudinaryService _cloudinaryService;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<UploadServicePhotoCommand> _localizer;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                ICloudinaryService cloudinaryService,
                ICacheService cacheService,
                IStringLocalizer<UploadServicePhotoCommand> localizer)
            {
                _dataContext = dataContext;
                _cloudinaryService = cloudinaryService;
                _cacheService = cacheService;
                _localizer = localizer;
            }

            public async Task<UploadServicePhotoModel> Handle(UploadServicePhotoCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Upload Service Photo for ServiceId: {model.Id}");

                if (model.File == null || model.File.Length == 0)
                {
                    throw new ArgumentException(_localizer["photo_upload.file.required"]);
                }

                // 1. Validate service exists
                var service = await _dataContext.VcServices.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken);
                if (service == null)
                {
                    throw new ArgumentException(_localizer["data.not-found"]);
                }

                // 2. Upload photo to Cloudinary
                var imageUrl = await _cloudinaryService.AddPhotoAsync(model.File);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    throw new Exception("Failed to upload photo.");
                }

                // 3. Update the service's ImageUrl in the database
                service.ImageUrl = imageUrl;
                _dataContext.VcServices.Update(service);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // 4. Invalidate cache
                _cacheService.Remove(PhotoUploadConstant.BuildCacheKey(service.Id.ToString()));
                _cacheService.Remove(PhotoUploadConstant.BuildCacheKey());

                Log.Information($"Successfully uploaded photo for ServiceId: {model.Id}. New ImageUrl: {imageUrl}");

                // 5. Return the updated model
                return model with { ImageUrl = imageUrl };
            }
        }
    }
}
