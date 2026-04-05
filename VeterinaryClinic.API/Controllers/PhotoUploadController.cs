using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/photo-upload")]
    [ApiExplorerSettings(GroupName = "10. Tải ảnh")]
    public class PhotoUploadController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PhotoUploadController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Service

        /// <summary>
        /// Tải ảnh lên cho một dịch vụ
        /// </summary>
        /// <param name="model">Chứa ID của dịch vụ và file ảnh</param>
        /// <returns>Thông tin ảnh đã tải lên</returns>
        [HttpPost, Route("service")]
        [ProducesResponseType(typeof(ResponseObject<UploadServicePhotoModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadPhoto([FromForm] UploadServicePhotoModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send( new UploadServicePhotoCommand(model));
            });
        }

        #endregion

        #region Pet
        
        /// <summary>
        /// Tải ảnh lên cho một thú cưng
        /// </summary>
        /// <param name="model">Chứa ID của thú cưng và file ảnh</param>
        /// <returns>Thông tin ảnh đã tải lên</returns>
        [HttpPost, Route("pet")]
        [ProducesResponseType(typeof(ResponseObject<UploadPetPhotoModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadPhoto([FromForm] UploadPetPhotoModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UploadPetPhotoCommand(model));
            });
        }
        
        #endregion

        #region User

        [HttpPost, Route("user")]
        [ProducesResponseType(typeof(ResponseObject<UploadUserPhotoModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadPhoto([FromForm] UploadUserPhotoModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UploadUserPhotoCommand(model));
            });
        }
        
        #endregion
    }
}
