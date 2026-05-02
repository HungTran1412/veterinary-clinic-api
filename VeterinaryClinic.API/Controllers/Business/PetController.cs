using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/pets")]
    [ApiExplorerSettings(GroupName = "06. Thú cưng (Quản lý thú cưng)")]
    [Authorize] 
    public class PetController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PetController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Thêm mới thú cưng
        /// </summary>
        /// <param name="model">Thông tin thú cưng</param>
        /// <returns>ID của thú cưng mới</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreatePetModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreatePetCommand(model));
            });
        }

        /// <summary>
        /// Cập nhật thông tin thú cưng
        /// </summary>
        /// <param name="id">ID của thú cưng</param>
        /// <param name="model">Thông tin cần cập nhật</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdatePetModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UpdatePetCommand(id, model));
            });
        }

        /// <summary>
        /// Xóa thú cưng (xóa mềm)
        /// </summary>
        /// <param name="id">ID của thú cưng</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeletePetCommand(id));
            });
        }

        /// <summary>
        /// Lấy thông tin thú cưng theo ID
        /// </summary>
        /// <param name="id">ID của thú cưng</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<PetModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetPetByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách thú cưng
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<PetModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] PetFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterPetQuery(filter));
            });
        }
        
        /// <summary>
        /// Lấy danh sách thú cưng cho combobox
        /// </summary>
        /// <param name="ownerId">Lọc theo ID chủ sở hữu (tùy chọn)</param>
        /// <response code="200">Thành công</response>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox([FromQuery] int? ownerId)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetComboboxPetQuery(ownerId));
            });
        }
    }
}
