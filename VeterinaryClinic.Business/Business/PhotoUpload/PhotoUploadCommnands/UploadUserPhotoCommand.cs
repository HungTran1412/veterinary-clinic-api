using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Business;

public class UploadUserPhotoCommand : IRequest<UploadUserPhotoModel>
{
    public UploadUserPhotoModel Model { get; }

    /// <summary>
    /// Tải ảnh lên cho người dùng
    /// </summary>
    /// <param name="model"></param>
    public UploadUserPhotoCommand(UploadUserPhotoModel model)
    {
        Model = model;
    }

    public class Handler : IRequestHandler<UploadUserPhotoCommand, UploadUserPhotoModel>
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

        public async Task<UploadUserPhotoModel> Handle(UploadUserPhotoCommand request,
            CancellationToken cancellationToken)
        {
            var model = request.Model;
            Log.Information($"Upload User Photo for User: {model.Id}");

            if (model.File == null || model.File.Length == 0)
            {
                throw new ArgumentException(_localizer["photo_upload.file.required"]);
            }
            
            //Kiem tra nguoi dung co ton tai khong
            var data = await _dataContext.VcUsers.FirstOrDefaultAsync(x => x.Id == model.Id);

            if (data == null)
            {
                throw new ArgumentException(_localizer["data.not-found"]);
            }
            
            //upload photo to cloudinary
            var avatarUrl = await _cloudinaryService.AddPhotoAsync(model.File);

            if (string.IsNullOrEmpty(avatarUrl))
            {
                throw new Exception("Failed to upload photo.");
            }
            
            //cap nhat du lieu vao db
            data.AvatarUrl = avatarUrl;
            _dataContext.VcUsers.Update(data);
            await _dataContext.SaveChangesAsync(cancellationToken);
            
            //xoa cache
            _cacheService.Remove(PhotoUploadConstant.BuildCacheKey(data.Id.ToString()));
            _cacheService.Remove(PhotoUploadConstant.BuildCacheKey());
            
            Log.Information($"Succesfully uploaded photo for UserId: {model.Id}");

            return model with { AvatarUrl = avatarUrl };
        }
    }
}