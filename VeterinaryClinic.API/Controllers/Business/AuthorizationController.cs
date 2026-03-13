using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/auth")]
    [ApiExplorerSettings(GroupName = "00. Xác thực (Authorization)")]
    public class AuthorizationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public AuthorizationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Đăng nhập hệ thống
        /// </summary>
        /// <param name="model">Thông tin đăng nhập</param>
        /// <returns>Token và thông tin người dùng</returns>
        [HttpPost, Route("login")]
        [ProducesResponseType(typeof(ResponseObject<LoginResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] UserLoginModel model)
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
        [HttpPost, Route("refresh-token")]
        [ProducesResponseType(typeof(ResponseObject<LoginResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new RefreshTokenCommand(model));
            });
        }
    }
}
