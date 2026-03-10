using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/specializations")]
    [ApiExplorerSettings(GroupName = "01. Chuyên ngành (Quản lý chuyên ngành)")]
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
                await _mediator.Send(new CreateSpecializationCommand(model));
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
                model.Id = id;
                return await _mediator.Send(new UpdateSpecializationCommand(model));
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
        #endregion
    }   
}