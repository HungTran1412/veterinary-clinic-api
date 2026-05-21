using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/shift-templates")]
    [ApiExplorerSettings(GroupName = "15. Quản lý mẫu ca làm việc (Quản trị viên)")]
    // [Authorize]
    public class ShiftTemplateController: ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ShiftTemplateController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD
        /// <summary>
        /// Thêm mới ca làm
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateShiftTemplateModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateShiftTemplateCommand(model));
            });
        }

        /// <summary>
        /// Cập nhật thông tin ca lamf
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateShiftTemplateModel model)
        {
            return await ExecuteFunction(async () =>
            {
                var updatedModel = model with { Id = id };
                return await _mediator.Send(new UpdateShiftTemplateCommand(updatedModel));
            });
        }

        /// <summary>
        /// Xóa ca làm
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeleteShiftTemplateCommand(id));
            });
        }

        /// <summary>
        /// Lấy thông tin chi tiết
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetShiftTemplateByIdQuery(id));
            });
        }

        /// <summary>
        /// Lấy danh sách ca làm
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<ShiftTemplateSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] ShiftTemplateFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterShiftTemplateQuery(filter));
            });
        }
        
        /// <summary>
        /// combobox
        /// </summary>
        /// <param name="count"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<ShiftTemplateSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async () =>
            {
                var rs = await _mediator.Send(new GetComboboxShiftTemplateQuery(count, ts));

                return rs;
            });
        }
        
        #endregion
    }
}