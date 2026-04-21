using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Busines;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/authorization")]
    [ApiExplorerSettings(GroupName = "00. Xác thực (Authorization)")]
    public class AuthorizationController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly MailSettings _mailSettings;

        public AuthorizationController(IMediator mediator, IOptions<MailSettings> mailSettings)
        {
            _mediator = mediator;
            _mailSettings = mailSettings.Value;
        }

        /// <summary>
        /// Đăng ký tài khoản khách hàng
        /// </summary>
        /// <param name="model">Thông tin đăng ký</param>
        /// <returns></returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ResponseObject<UserRegisterResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UserRegisterCommand(model));
            });
        }

        /// <summary>
        /// Xác thực email và kích hoạt tài khoản (Dành cho người dùng bấm link từ email)
        /// </summary>
        /// <param name="token">Token được gửi qua email</param>
        /// <returns>JSON response chuẩn</returns>
        [HttpGet("verify-email")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new VerifyEmailQuery(token));
            });
        }

        /// <summary>
        /// Đăng nhập hệ thống
        /// </summary>
        /// <param name="model">Thông tin đăng nhập</param>
        /// <returns>Token và thông tin người dùng</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ResponseObject<LoginResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UserLoginCommand(model));
            });
        }
        
        /// <summary>
        /// Làm mới Access Token
        /// </summary>
        /// <param name="model">Access Token cũ và Refresh Token</param>
        /// <returns>Token mới</returns>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ResponseObject<LoginResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new RefreshTokenCommand(model));
            });
        }

        /// <summary>
        /// Đăng xuất hệ thống
        /// </summary>
        /// <param name="model">Refresh Token cần vô hiệu hóa</param>
        /// <returns>Kết quả đăng xuất</returns>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] LogoutModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new UserLogoutCommand(model));
            });
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của Access Token hiện tại
        /// </summary>
        /// <returns>Thông tin người dùng nếu token hợp lệ</returns>
        [HttpGet("check-token")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<CheckTokenResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckToken()
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CheckTokenQuery());
            });
        }
    }
}
