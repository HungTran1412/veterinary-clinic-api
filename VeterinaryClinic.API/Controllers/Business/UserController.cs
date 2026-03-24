using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/user-manager")]
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

        /// <summary>
        /// Cập nhật thông tin người dùng
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <param name="model">Thông tin cần cập nhật</param>
        /// <returns></returns>
        [HttpPatch, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUserModel model)
        {
            return await ExecuteFunction(async () =>
            {
                model.Id = id;
                return await _mediator.Send(new UpdateUserCommand(model));
            });
        }

        /// <summary>
        /// Xóa tài khoản người dùng (Xóa mềm)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new DeleteUserCommand(id));
            });
        }

        /// <summary>
        /// Lấy thông tin người dùng theo ID
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<UserModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetUserByIdQuery(id));
            });
        }

        /// <summary>
        /// Lọc danh sách người dùng
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<PaginationList<UserModel>>), StatusCodes.Status200OK)] 
        public async Task<IActionResult> Filter([FromBody] UserFilterModel filter)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetFilterUserQuery(filter));
            });
        }

        /// <summary>
        /// Đổi mật khẩu (Người dùng tự đổi)
        /// </summary>
        /// <param name="model">Thông tin đổi mật khẩu</param>
        /// <returns></returns>
        [HttpPost, Route("change-password")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] UpdatePasswordUserModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new ChangePasswordUserCommand(model));
            });
        }

        #endregion
    }
}
