using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/medical-records")]
    [ApiExplorerSettings(GroupName = "08. Hồ sơ khám bệnh (Quản lý hồ sơ khám bệnh)")]
    // [Authorize]
    public class MedicalRecordController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public MedicalRecordController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Thêm mới hồ sơ khám bệnh
        /// </summary>
        /// <param name="model">Thông tin hồ sơ khám bệnh</param>
        /// <returns>ID của hồ sơ mới</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateMedicalRecordModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateMedicalRecordCommand(model));
            });
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ khám bệnh
        /// </summary>
        /// <param name="id">ID của hồ sơ</param>
        /// <param name="model">Thông tin cần cập nhật</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateMedicalRecordModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UpdateMedicalRecordCommand(id, model));
            });
        }

        /// <summary>
        /// Lấy thông tin hồ sơ khám bệnh theo ID
        /// </summary>
        /// <param name="id">ID của hồ sơ</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<MedicalRecordModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetMedicalRecordByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách hồ sơ khám bệnh
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<MedicalRecordModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] MedicalRecordFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterMedicalRecordQuery(filter));
            });
        }
    }
}
