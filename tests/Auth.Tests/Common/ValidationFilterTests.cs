using Auth.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Net;

namespace Auth.Tests.Common;

public class ValidationFilterTests
{
    private static ActionExecutingContext ExecutingContext(ModelStateDictionary modelState)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), modelState);

        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: null!);
    }

    [Fact]
    public void ValidRequest_IsLetThrough()
    {
        var context = ExecutingContext(new ModelStateDictionary());

        new ValidationFilter().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void InvalidRequest_IsShortCircuitedWithAValidationFailure()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Username", "The Username field is required.");
        var context = ExecutingContext(modelState);

        new ValidationFilter().OnActionExecuting(context);

        var result = Assert.IsType<ValidationFailedResult>(context.Result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
    }

    [Fact]
    public void OnActionExecuted_DoesNothing()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var context = new ActionExecutedContext(actionContext, [], controller: null!);

        new ValidationFilter().OnActionExecuted(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void FailedResult_WrapsTheValidationDto()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "The Email field is not a valid e-mail address.");

        var dto = Assert.IsType<ValidationFailedDto>(new ValidationFailedResult(modelState).Value);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, dto.StatusCode);
        Assert.Equal("REQUEST_NOT_VALID", dto.Code);
        Assert.Equal("Validation of request failed", dto.Message);
    }

    [Fact]
    public void FailedDto_ListsEveryModelStateErrorWithItsField()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Username", "required");
        modelState.AddModelError("Email", "not an address");
        modelState.AddModelError("Email", "too long");

        var dto = new ValidationFailedDto(modelState);

        Assert.Equal(3, dto.Errors.Count);
        Assert.Equal(2, dto.Errors.Count(e => e.Field == "Email"));
        Assert.Contains(dto.Errors, e => e.Field == "Username" && e.Message == "required");
    }

    [Fact]
    public void FailedDto_DefaultsTheErrorCodeToFiftyFive()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Username", "required");

        Assert.Equal(55, new ValidationFailedDto(modelState).Errors[0].Code);
    }

    [Fact]
    public void FailedDto_ReportsNoFieldForAnErrorAttachedToTheWholeRequest()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "request body could not be read");

        Assert.Null(new ValidationFailedDto(modelState).Errors[0].Field);
    }

    [Fact]
    public void FailedDto_HasNoErrorsWhenModelStateIsClean()
    {
        Assert.Empty(new ValidationFailedDto(new ModelStateDictionary()).Errors);
    }

    [Fact]
    public void ValidationError_KeepsANonDefaultCode()
    {
        Assert.Equal(400, new ValidationError("Username", 400, "required").Code);
    }
}
