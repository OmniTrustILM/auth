using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Auth.Common.Exceptions
{
    public class ErrorDetails
    {
        [JsonPropertyOrder(-3)]
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; private set; }

        [JsonPropertyOrder(-2)]
        [JsonPropertyName("code")]
        public string Code { get; private set; }

        [JsonPropertyOrder(-1)]
        [JsonPropertyName("message")]
        public string Message { get; private set; }

        public ErrorDetails(Exception exception)
        {
            if(exception is RequestException requestException)
            {
                StatusCode = (int)requestException.StatusCode;
                Code = requestException.Code;
                Message = requestException.Message;
            }
            else
            {
                StatusCode = (int)HttpStatusCode.InternalServerError;
                Code = "APPLICATION_ERROR";
                Message = "Application Error";
            }
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
