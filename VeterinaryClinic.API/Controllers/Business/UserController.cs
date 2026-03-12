using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/users")]
    [ApiExplorerSettings(GroupName = "03. Người dùng (Quản lý người dùng)")]
    public class UserController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region CRUD

        /// <summary>
        /// Tạo mới tài khoản người dùng (Dành cho Admin)
        /// </summary>
        /// <param name="model">Thông tin người dùng cần tạo</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateUserModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateUserCommand(model));
            });
        }

        #endregion
    }
}
