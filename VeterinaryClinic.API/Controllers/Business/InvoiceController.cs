using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/invoices")] // Đã sửa lại route thành invoices
    [ApiExplorerSettings(GroupName = "13. Hóa đơn")]
    public class InvoiceController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InvoiceController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        #region CRUD

        /// <summary>
        /// Thêm mới hóa đơn
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new CreateInvoiceCommand(model));
            });
        }

        /// <summary>
        /// Lấy thông tin hóa đơn theo id
        /// </summary>
        /// <param name="id">id hóa đơn</param>
        /// <returns></returns>
        [HttpGet, Route("{id}")]
        [ProducesResponseType(typeof(ResponseObject<InvoiceModel>), StatusCodes.Status200OK)] // Đã sửa kiểu trả về thành InvoiceModel
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetInvoiceByIdQuery(id));
            });
        }

        #endregion
    }
}
