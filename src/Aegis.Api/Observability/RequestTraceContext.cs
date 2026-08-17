using System.Diagnostics;

namespace Aegis.Api.Observability;

public static class RequestTraceContext
{
    public static string GetTraceId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
