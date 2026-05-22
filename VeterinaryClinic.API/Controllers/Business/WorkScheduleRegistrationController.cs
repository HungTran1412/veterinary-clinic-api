using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/work-schedule-registrations")]
    [ApiExplorerSettings(GroupName = "16. Dang ky lich lam viec")]
    [Authorize]
    public class WorkScheduleRegistrationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public WorkScheduleRegistrationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateWorkScheduleRegistrationModel model)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new CreateWorkScheduleRegistrationCommand(model)));
        }

        [HttpPost("{id}/process")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Process([FromRoute] int id, [FromBody] ProcessWorkScheduleRegistrationModel model)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new ProcessWorkScheduleRegistrationCommand(id, model)));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new DeleteWorkScheduleRegistrationCommand(id)));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseObject<WorkScheduleRegistrationModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new GetWorkScheduleRegistrationQueryByIdQuery(id)));
        }

        [HttpPost("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<WorkScheduleRegistrationModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] WorkScheduleRegistrationFilterModel filter)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new GetFilterWorkScheduleRegistrationQuery(filter)));
        }
    }
}
