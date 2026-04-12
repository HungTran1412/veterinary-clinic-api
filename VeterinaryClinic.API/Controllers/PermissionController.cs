using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{

    [ApiController]
    [Route("veterinary-clinic/v1/permission")]
    [ApiExplorerSettings(GroupName = "101. Permission (Danh mục quyền)")]
    [Authorize]
    public class PermissionController : ApiControllerBase
    {
        public readonly IMediator _mediator;

        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách quyền người dùng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <response code="200">Thành công</response>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<PermissionSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetComboboxPermissionQuery(count, ts));
            });
        }
    }
}
