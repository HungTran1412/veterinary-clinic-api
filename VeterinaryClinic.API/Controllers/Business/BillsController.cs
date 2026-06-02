using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
    [ApiController]
    [Route("veterinary-clinic/v1/bills")]
    [ApiExplorerSettings(GroupName = "18. Hóa đơn tổng")]
    // [Authorize(Roles = $"{Role.ADMIN},{Role.RECEPTIONIST}")]
    public class BillsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public BillsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một hóa đơn tổng để thanh toán cho nhiều dịch vụ/lịch hẹn cùng lúc.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBill([FromBody] CreateBillModel model)
        {
            return await ExecuteFunction(async () => await _mediator.Send(new CreateBillCommand(model)));
        }

        /// <summary>
        /// Xuất file PDF cho một hóa đơn tổng đã tồn tại.
        /// </summary>
        // [HttpGet("export/{id}")]
        // [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        // [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status404NotFound)]
        // public async Task<IActionResult> GetBillPdf([FromRoute] int id)
        // {
        //     var pdfBytes = await _mediator.Send(new GenerateBillPdfQuery(id));
        //
        //     if (pdfBytes == null || pdfBytes.Length == 0)
        //     {
        //         return NotFound(new ResponseObject<object>(null, "Could not generate PDF."));
        //     }
        //
        //     return File(pdfBytes, "application/pdf", $"bill-{id}.pdf");
        // }

        /// <summary>
        /// Xuất file PDF cho một lịch hẹn (tự động tìm hoặc tạo hóa đơn tổng).
        /// </summary>
        [HttpGet("export/{appointmentId}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GeneratePdfFromAppointment([FromRoute] int appointmentId)
        {
            var pdfBytes = await _mediator.Send(new GeneratePdfForAppointmentQuery(appointmentId));

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return NotFound(new ResponseObject<object>(null, "Could not generate PDF."));
            }

            Response.Headers[HeaderNames.ContentDisposition] = $"inline; filename=bill-for-appointment-{appointmentId}.pdf";
            return File(pdfBytes, "application/pdf");
        }
    }
}
