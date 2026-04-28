using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/appointments")]
    [ApiExplorerSettings(GroupName = "09. Đặt lịch khám (Quản lý lịch khám)")]
    // [Authorize]
    public class AppointmentController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới lịch hẹn khám.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateAppointmentCommand(model));
            });
        }

        /// <summary>
        /// Xử lý chuyển trạng thái lịch hẹn.
        /// </summary>
        [HttpPost("{id}/process")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Process([FromRoute] int id, [FromBody] ProcessAppointmentModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new ProcessAppointmentCommand(id, model));
            });
        }

        /// <summary>
        /// Lấy thông tin lịch hẹn theo id
        /// </summary>
        /// <param name="id">id lịch hẹn</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<AppointmentModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetAppointmentByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách lịch hẹn
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<AppointmentModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] AppoinntmentFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterAppointmentQuery(filter));
            });
        }
    }
}
