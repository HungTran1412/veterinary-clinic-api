using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/notifications")]
    [ApiExplorerSettings(GroupName = "17. Thông báo")]
    [Authorize]
    public class NotificationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(ResponseObject<List<NotificationModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetAllNotificationQuery());
            });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseObject<NotificationModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetNotificationByIdQuery(id));
            });
        }

        [HttpPost("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<NotificationModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] NotificationFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterNotificationQuery(filter));
            });
        }

        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAsRead([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new ProcessNotifcationCommand(id));
            });
        }
    }
}
