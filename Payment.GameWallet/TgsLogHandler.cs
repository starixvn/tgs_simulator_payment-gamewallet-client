using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Tgs.Logging;

namespace Payment.GameWallet;

public class TgsLogHandler(ITgsLog tgsLog)
{
    public void LogApi(HttpContext context, object? request, object? response, Guid trackingId)
    {
        var jsonOption = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        var apiCall = new ApiLogEvent
        {
            Ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Method = HttpMethod.Post.Method,
            Path = context.Request.Path,
            Prefix = context.Request.GetDisplayUrl().Replace(context.Request.Path, string.Empty),
            RecordedOn = DateTimeOffset.Now,
            RequestContent = JsonSerializer.Serialize(request, jsonOption),
            RequestHeaders = JsonSerializer.Serialize(context.Request.Headers),
            ResponseCode = ((short)HttpStatusCode.OK),
            ResponseContent = JsonSerializer.Serialize(response),
            ResponseHeaders = JsonSerializer.Serialize(context.Response.Headers),
            ServerName = Environment.MachineName,
            TrackingId = trackingId
        };

        tgsLog.LogApi(apiCall);
    }
}
