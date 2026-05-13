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

        /// <summary>
        /// Tạo URL thanh toán VNPay cho lịch hẹn.
        /// </summary>
        [HttpPost("vnpay/create/{appointmentId}")]
        [ProducesResponseType(typeof(ResponseObject<VnPayPaymentUrlModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateVnPayPayment([FromRoute] int appointmentId)
        {
            return await ExecuteFunction(async () =>
            {
                var model = new CreateVnPayPaymentModel
                {
                    AppointmentId = appointmentId,
                    ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                return await _mediator.Send(new CreateVnPayPaymentCommand(model));
            });
        }

        /// <summary>
        /// Redirect trực tiếp sang trang thanh toán VNPay.
        /// </summary>
        [HttpGet("vnpay/redirect/{appointmentId}")]
        public async Task<IActionResult> RedirectToVnPay([FromRoute] int appointmentId)
        {
            var model = new CreateVnPayPaymentModel
            {
                AppointmentId = appointmentId,
                ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var result = await _mediator.Send(new CreateVnPayPaymentCommand(model));
            return Redirect(result.PaymentUrl);
        }

        /// <summary>
        /// Callback/return URL từ VNPay.
        /// </summary>
        [HttpGet("vnpay/return")]
        [ProducesResponseType(typeof(ResponseObject<VnPayReturnModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VnPayReturn()
        {
            return await ExecuteFunction(async () =>
            {
                var queryData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
                return await _mediator.Send(new ProcessVnPayReturnCommand(queryData));
            });
        }
    }
}
