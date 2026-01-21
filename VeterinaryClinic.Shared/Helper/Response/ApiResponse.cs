using System.Text.Json.Serialization;

namespace VeterinaryClinic.Shared.Helper.Response;

public class ApiResponse<T>
{
    public string TraceId { get; set; } = string.Empty;
    public int Code { get; set; } = 200;
    public string Message { get; set; } = string.Empty;
    public double RequestDuration { get; set; }
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(T data, string message = "Success", int code = 200, string traceId = "", double duration = 0)
    {
        Data = data;
        Message = message;
        Code = code;
        TraceId = traceId;
        RequestDuration = duration;
    }

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>(data, message);
    }

    public static ApiResponse<T> Fail(string message, int code = 400)
    {
        return new ApiResponse<T>(default, message, code);
    }
}

// Class non-generic cho trường hợp không có data trả về
public class ApiResponse : ApiResponse<object>
{
    public ApiResponse() { }

    public ApiResponse(object data, string message = "Success", int code = 200, string traceId = "", double duration = 0) 
        : base(data, message, code, traceId, duration)
    {
    }

    public static new ApiResponse Success(object? data = null, string message = "Success")
    {
        return new ApiResponse(data ?? new object(), message);
    }

    public static new ApiResponse Fail(string message, int code = 400)
    {
        return new ApiResponse(new object(), message, code);
    }
}