using Aegis.Authorization.Core.Metrics;
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
        public ActionResult<MetricsSnapshot> GetAuthorizationMetrics()
        {
            var snapshot = _metrics.Snapshot();
            return Ok(snapshot);
        }
    }
}
