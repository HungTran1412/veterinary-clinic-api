using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/email-logs")]
    [ApiExplorerSettings(GroupName = "04. Nhật ký gửi mail")]
    // [Authorize]
    public class EmailLogController : ApiControllerBase
    {
        public readonly IMediator _mediator;

        public EmailLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD
        /// <summary>
        /// Lọc danh sách email đã gửi
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<EmailLogModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] EmailLogFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterEmailLogQuery(filter));
            });
        }

        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetEmailLogByIdQuery(id));
            }); 
        }
        #endregion
    }
}