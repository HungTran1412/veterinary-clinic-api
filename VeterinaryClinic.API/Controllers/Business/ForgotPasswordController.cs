using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/forgot-password")]
    [ApiExplorerSettings(GroupName = "11. Quên mật khẩu")]
    public class ForgotPasswordController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ForgotPasswordController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gửi mã OTP để đặt lại mật khẩu.
        /// </summary>
        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(ResponseObject<SendOtpResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOtp([FromBody] ForgotPasswordModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new SendOtpCommand(model));
            });
        }

        /// <summary>
        /// Xác thực mã OTP.
        /// </summary>
        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new VerifyOtpCommand(model));
            });
        }

        /// <summary>
        /// Đặt lại mật khẩu sau khi đã xác thực OTP.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new ResetPasswordCommand(model));
            });
        }
    }
}
