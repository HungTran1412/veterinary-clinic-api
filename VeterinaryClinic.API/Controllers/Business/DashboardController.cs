using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers
{
 
    [ApiController]
    [Route("veterinary-clinic/v1/dashboard")]
    [ApiExplorerSettings(GroupName = "14. Dashboard (thống kê)")]
    public class DashboardController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Thong ke tong quan
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("overview")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Overview()
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetDashboardOverviewStatisticQuery());
            });
        }
        
        /// <summary>
        /// Thong ke doanh thu
        /// </summary>
        /// <param name="Month"></param>
        /// <returns></returns>
        [HttpPost, Route("revenue-overview")]
        [ProducesResponseType(typeof(ResponseObject<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RevenueOverview([FromBody]RevenueOverviewRequestModel model)
        {
            return await ExecuteFunction(async () =>
            {
                return await _mediator.Send(new GetDoashboardRevenueOverviewQuery(model));
            });
        }
    }   
}