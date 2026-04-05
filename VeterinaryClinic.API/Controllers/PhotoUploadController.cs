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

        /// <summary>
        /// Tải ảnh lên cho một dịch vụ
        /// </summary>
        /// <param name="model">Chứa ID của dịch vụ và file ảnh</param>
        /// <returns>Thông tin ảnh đã tải lên</returns>
        [HttpPost("service")]
        [ProducesResponseType(typeof(ResponseObject<UploadServicePhotoModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadServicePhoto([FromForm] UploadServicePhotoModel model)
        {
            return await ExecuteFunction(async () =>
            {
                var command = new UploadServicePhotoCommand(model);
                return await _mediator.Send(command);
            });
        }
    }
}
