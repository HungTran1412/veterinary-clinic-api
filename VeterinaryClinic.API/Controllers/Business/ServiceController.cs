using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/services")]
    [ApiExplorerSettings(GroupName = "02. Dịch vụ (Quản lý dịch vụ)")]
    // [Authorize]
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
        // [ClaimRequirement(ClaimConstants.PERMISSIONS, nameof(PermissionVeterinaryClinicEnum.SERVICE_MANAGER_ADD))]
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
        // [ClaimRequirement(ClaimConstants.PERMISSIONS, nameof(PermissionVeterinaryClinicEnum.SERVICE_MANAGER_EDIT))]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateServiceModel model)
        {
            return await ExecuteFunction(async () =>
            {
                var updatedModel = model with { Id = id };
                return await _mediator.Send(new UpdateServiceCommand(updatedModel));
            });
        }

        /// <summary>
        /// Xóa dịch vụ
        /// </summary>
        /// <param name="id">id dịch vụ</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        // [ClaimRequirement(ClaimConstants.PERMISSIONS, nameof(PermissionVeterinaryClinicEnum.SERVICE_MANAGER_DELETE))]
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
        // [ClaimRequirement(ClaimConstants.PERMISSIONS, nameof(PermissionVeterinaryClinicEnum.SERVICE_MANAGER_VIEW))]
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
        // [ClaimRequirement(ClaimConstants.PERMISSIONS, nameof(PermissionVeterinaryClinicEnum.SERVICE_MANAGER_VIEW))]
        [ProducesResponseType(typeof(ResponseObject<List<ServiceSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] ServiceFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterServiceQuery(filter));
            });
        }
        
        /// <summary>
        /// Lọc danh sách dịch vụ được sử dụng nhiều nhất.
        /// </summary>
        /// <param name="filter">Điều kiện phân trang.</param>
        /// <returns></returns>
        [HttpPost, Route("filter-top")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<TopServiceModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FilterTop([FromBody] BaseQueryFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterTopServiceQuery(filter));
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
                return await _mediator.Send(new GetComboboxServiceQuery(count, ts));
            });
        }
        
        #endregion
    }   
}
