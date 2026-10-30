using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApexPost.Web.Filters;

public sealed class ValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var isLoginPost = context.HttpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.ActionDescriptor.RouteValues["controller"], "Account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.ActionDescriptor.RouteValues["action"], "Login", StringComparison.OrdinalIgnoreCase);

        if (!context.ModelState.IsValid && context.HttpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) && !isLoginPost)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed."
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
