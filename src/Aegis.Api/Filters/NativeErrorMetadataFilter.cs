using Aegis.Api.Observability;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aegis.Api.Filters;

/// <summary>
/// Ensures every native MVC error envelope carries a correlation identifier
/// and exposes its stable code to request-completion logging.
/// </summary>
public sealed class NativeErrorMetadataFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: IApiResponse { Success: false, Error: not null } response })
        {
            var traceId = RequestTraceContext.GetTraceId(context.HttpContext);
            response.Error = response.Error with { TraceId = response.Error.TraceId ?? traceId };
            context.HttpContext.Items["Aegis.ErrorCode"] = response.Error.Code;
        }

        return next();
    }
}
