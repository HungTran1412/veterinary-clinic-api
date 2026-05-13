using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{

    [ApiController]
    [Route("veterinary-clinic/v1/payments")]
    [ApiExplorerSettings(GroupName = "12. Thanh toán")]
    public class PaymentController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD

        /// <summary>
        /// Thêm mới thanh toán
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreatePaymentCommand(model));
            });
        }

        /// <summary>
        /// Lấy thông tin thanh toán theo id
        /// </summary>
        /// <param name="id">id lịch hẹn</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<AppointmentModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetPaymentByIdQuery(id));
            });
        }

        #endregion
    }
}