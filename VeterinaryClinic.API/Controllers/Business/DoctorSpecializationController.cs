using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/doctor-specializations")]
    [ApiExplorerSettings(GroupName = "07. Chuyên ngành của Bác sĩ")]
    [Authorize(Roles = "ADMIN")]
    public class DoctorSpecializationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public DoctorSpecializationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Thêm một hoặc nhiều chuyên ngành cho bác sĩ
        /// </summary>
        /// <param name="model">Thông tin bác sĩ và danh sách chuyên ngành</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] DoctorSpecializationModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateDoctorSpecializationCommand(model));
            });
        }
        
    }
}
