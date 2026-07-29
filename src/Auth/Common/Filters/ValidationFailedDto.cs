using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace Auth.Common.Filters
{
    public class ValidationFailedDto
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string Code { get; private set; }
        public string Message { get; }
        public List<ValidationError> Errors { get; }
        public ValidationFailedDto(ModelStateDictionary modelState)
        {
            Message = "Validation of request failed";
            StatusCode = HttpStatusCode.UnprocessableEntity;
            Code = "REQUEST_NOT_VALID";
            Errors = modelState.Keys
                    .SelectMany(key => modelState[key].Errors.Select(x => new ValidationError(key, 0, x.ErrorMessage)))
                    .ToList();
        }
    }

    public class ValidationError
    {
        public string Field { get; }
        public int Code { get; set; }
        public string Message { get; }
        public ValidationError(string field, int code, string message)
        {
            Field = field != string.Empty ? field : null;
            Code = code != 0 ? code : 55;  //set the default code to 55. you can remove it or change it to 400.
            Message = message;
        }
    }
}
