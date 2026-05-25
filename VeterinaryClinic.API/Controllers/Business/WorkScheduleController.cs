using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/work-schedule")]
    [ApiExplorerSettings(GroupName = "05. Lịch làm việc (Quản lý lịch làm việc)")]
    [Authorize]
    public class WorkScheduleController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public WorkScheduleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới lịch làm việc
        /// </summary>
        /// <param name="model">Thông tin lịch làm việc</param>
        /// <returns>ID của lịch làm việc mới</returns>
        [HttpPost, Route("many")]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMany([FromBody] List<CreateWorkScheduleModel> model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateManyWorkScheduleCommand(model));
            });
        }
        
        /// <summary>
        /// Tạo mới lịch làm việc
        /// </summary>
        /// <param name="model">Thông tin lịch làm việc</param>
        /// <returns>ID của lịch làm việc mới</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateWorkScheduleModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateWorkScheduleCommand(model));
            });
        }

        /// <summary>
        /// Cập nhật lịch làm việc
        /// </summary>
        /// <param name="id">ID của lịch làm việc</param>
        /// <param name="model">Thông tin cần cập nhật</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateWorkScheduleModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UpdateWorkScheduleCommand(id, model));
            });
        }

        /// <summary>
        /// Xóa lịch làm việc (Xóa mềm)
        /// </summary>
        /// <param name="id">ID của lịch làm việc</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeleteWorkScheduleCommand(id));
            });
        }

        /// <summary>
        /// Lấy thông tin lịch làm việc theo ID
        /// </summary>
        /// <param name="id">ID của lịch làm việc</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<WorkScheduleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetWorkScheduleByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách lịch làm việc
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkScheduleModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] WorkScheduleFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterWorkScheduleQuery(filter));
            });
        }
    }
}
