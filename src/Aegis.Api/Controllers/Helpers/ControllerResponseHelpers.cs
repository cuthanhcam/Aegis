using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers.Helpers
{
    internal static class ControllerResponseHelpers
    {
        public static ActionResult<ApiResponse<T>> OkResponse<T>(this ControllerBase controller, T value)
        {
            return new OkObjectResult(ApiResponse<T>.Ok(value));
        }

        public static ActionResult<ApiResponse<T>> CreatedResponse<T>(this ControllerBase controller, T value)
        {
            return new CreatedResult(string.Empty, ApiResponse<T>.Ok(value));
        }

        public static ActionResult<ApiResponse<T>> NotFoundResponse<T>(this ControllerBase controller, string code, string message)
        {
            return new NotFoundObjectResult(ApiResponse<T>.Fail(code, message));
        }

        public static ActionResult<ApiResponse<string>> DeletedResponse(this ControllerBase controller, bool deleted)
        {
            return deleted
                ? controller.OkResponse("deleted")
                : controller.NotFoundResponse<string>("NOT_FOUND", "Resource was not found.");
        }
    }
}
