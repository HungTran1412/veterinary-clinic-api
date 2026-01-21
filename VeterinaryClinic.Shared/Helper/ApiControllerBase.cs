using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinaryClinic.Shared.Helper.Response;

namespace VeterinaryClinic.Shared
{
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
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