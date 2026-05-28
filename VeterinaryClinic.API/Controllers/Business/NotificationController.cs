using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers;

[ApiController]
[Route("veterinary-clinic/v1/notifications")]
[ApiExplorerSettings(GroupName = "17. Thông báo")]
public class NotificationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// lay chuyen nganh theo id
    /// </summary>
    /// <param name="id">id chuyen nganh</param>
    /// <returns></returns>
    [HttpGet, Route("{id}")]
    [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        return await ExecuteFunction(async () =>
        {
            return await _mediator.Send(new GetNotificationByIdQuery(id));
        });
    }

    [HttpPost, Route("filter")]
    [ProducesResponseType(typeof(ResponseObject<List<NotificationModel>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filter([FromBody] NotificationFilterModel filter)
    {
        return await ExecuteFunction(async () =>
        {
            return await _mediator.Send(new GetFilterNotificationQuery(filter));
        });
    }
}