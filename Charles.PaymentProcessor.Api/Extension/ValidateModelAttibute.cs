using Charles.PaymentProcessor.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Charles.PaymentProcessor.Api.Extension;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.ModelState.IsValid) return;
        List<string> errorMessages = context.ModelState
            .Where(modelError => modelError.Value != null && modelError.Value.Errors.Any())
            .Select(modelError =>
                modelError.Value.Errors.FirstOrDefault()?.ErrorMessage).ToList();

        string validationSummary = string.Join("\n", errorMessages);

        ErrorResponse err = new()
            { Success = false, Status = StatusCodes.Status400BadRequest, Message = validationSummary };
        context.Result = new BadRequestObjectResult(err);

        base.OnResultExecuting(context);
    }
}