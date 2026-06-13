using Aegis.Authorization.Core.Metrics;
using Aegis.Api.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IAuthorizationMetrics _metrics;

        public MetricsController(IAuthorizationMetrics metrics)
        {
            _metrics = metrics;
        }

        [HttpGet("authorization")]
        public ContentResult GetAuthorizationMetrics()
        {
            return Content(PrometheusMetricsFormatter.Format(_metrics), PrometheusMetricsFormatter.ContentType);
        }
    }
}
