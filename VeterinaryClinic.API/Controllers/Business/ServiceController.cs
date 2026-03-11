using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/services")]
    [ApiExplorerSettings(GroupName = "02. Dịch vụ (Quản lý dịch vụ)")]
    public class ServiceController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ServiceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD
        /// <summary>
        /// Thêm mới dịch vụ
        /// </summary>
        /// <param name="model">Thông tin dịch vụ</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateServiceModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateServiceCommand(model));
            });
        }

        /// <summary>
        /// Cập nhật dịch vụ
        /// </summary>
        /// <param name="id">id dịch vụ</param>
        /// <param name="model">Thông tin dịch vụ cần cập nhật</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateServiceModel model)
        {
            return await ExecuteFunction(async () =>
            {
                model.Id = id;
                return await _mediator.Send(new UpdateServiceCommand(model));
            });
        }

        /// <summary>
        /// Xóa dịch vụ
        /// </summary>
        /// <param name="id">id dịch vụ</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeleteServiceCommand(id));
            });
        }

        /// <summary>
        /// Lấy thông tin dịch vụ theo id
        /// </summary>
        /// <param name="id">id dịch vụ</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetServiceByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách dịch vụ
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<ServiceSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] ServiceFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterServiceQuery(filter));
            });
        }
        
        /// <summary>
        /// Lấy danh sách dịch vụ cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <response code="200">Thành công</response>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<ServiceSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async () =>
            {
                var rs = await _mediator.Send(new GetComboboxServiceQuery(count, ts));
                return rs;
            });
        }
        
        #endregion
    }   
}