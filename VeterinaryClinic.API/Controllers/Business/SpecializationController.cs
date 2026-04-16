using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/specializations")]
    [ApiExplorerSettings(GroupName = "01. Chuyên ngành (Quản lý chuyên ngành)")]
    // [Authorize]
    public class SpecializationController: ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SpecializationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD
        /// <summary>
        /// Thêm mới chuyên ngành
        /// </summary>
        /// <param name="model">Thông tin chuyên ngành</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateSpecializationModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateSpecializationCommand(model));
            });
        }

        /// <summary>
        /// Cap nhat chuyen nganh
        /// </summary>
        /// <param name="id">id chuyen nganh</param>
        /// <param name="model">Thong tin chuyen nganh can cap nhat</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateSpecializationModel model)
        {
            return await ExecuteFunction(async () =>
            {
                var updatedModel = model with { Id = id };
                return await _mediator.Send(new UpdateSpecializationCommand(updatedModel));
            });
        }

        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeleteSpecializationCommand(id));
            });
        }

        /// <summary>
        /// lay danh sach chuyen nganh theo id
        /// </summary>
        /// <param name="id">id chuyen nganh</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetSpecializationByIdQuery(id));
            });
        }

        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<SpecializationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] SpecializationFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterSpecializationQuery(filter));
            });
        }
        
        /// <summary>
        /// Lay danh sach chuyen nganh cho combobox
        /// </summary>
        /// <param name="count">So ban ghi toi da</param>
        /// <param name="ts">Tu khoa tim kiem</param>
        /// <response code="200">Thành công</response>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SpecializationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async () =>
            {
                var rs = await _mediator.Send(new GetComboboxSpecializationQuery(count, ts));

                return rs;
            });
        }
        
        #endregion
    }   
}