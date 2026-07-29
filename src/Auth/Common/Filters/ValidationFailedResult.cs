using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Auth.Common.Filters
{
    public class ValidationFailedResult : ObjectResult
    {
        public ValidationFailedResult(ModelStateDictionary modelState)
             : base(new ValidationFailedDto(modelState))
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity; //change the http status code to 422.
        }
    }
}
