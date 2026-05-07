using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace VeterinaryClinic.Shared
{
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
        protected readonly Func<IContextAccessor> _contextAccessorFactory;
        protected readonly IMediator _mediator;
        protected readonly IStringLocalizer<Resources> _localizer;
        protected readonly IConfiguration _config;

        // Constructor for controllers that need dependencies
        public ApiControllerBase(Func<IContextAccessor> contextAccessorFactory, IMediator mediator, IStringLocalizer<Resources> localizer, IConfiguration config)
        {
            _contextAccessorFactory = contextAccessorFactory;
            _mediator = mediator;
            _localizer = localizer;
            _config = config;
        }

        public ApiControllerBase()
        {
        }

        protected async Task<IActionResult> ExecuteFunction<T>(Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await action();
                stopwatch.Stop();

                return Ok(new ApiResponse<T>(
                    data: result,
                    message: "Success",
                    code: 200,
                    traceId: HttpContext.TraceIdentifier,
                    duration: stopwatch.Elapsed.TotalMilliseconds
                ));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Log error here if needed
                return BadRequest(new ApiResponse<object>(
                    data: null,
                    message: ex.Message,
                    code: 400,
                    traceId: HttpContext.TraceIdentifier,
                    duration: stopwatch.Elapsed.TotalMilliseconds
                ));
            }
        }

        // Overload cho trường hợp không trả về dữ liệu (void/Task)
        protected async Task<IActionResult> ExecuteFunction(Func<Task> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
                stopwatch.Stop();

                return Ok(new ApiResponse<object>(
                    data: null,
                    message: "Success",
                    code: 200,
                    traceId: HttpContext.TraceIdentifier,
                    duration: stopwatch.Elapsed.TotalMilliseconds
                ));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BadRequest(new ApiResponse<object>(
                    data: null,
                    message: ex.Message,
                    code: 400,
                    traceId: HttpContext.TraceIdentifier,
                    duration: stopwatch.Elapsed.TotalMilliseconds
                ));
            }
        }
    }   
}