using System.Text.Json;
using System.Text.Json.Serialization;

namespace Auth.Common.Exceptions
{
    public class ErrorDetailsExtended : ErrorDetails
    {

        [JsonPropertyName("url")]
        public string Url { get; private set; }

        [JsonPropertyName("service")]
        public string Service { get; private set; }
        
        [JsonPropertyName("exception")]
        public string[] Exception { get; private set; }

        [JsonPropertyName("innerException")]
        public string[]? InnerException { get; private set; }

        public ErrorDetailsExtended(string url, string service, Exception exception)
            : base(exception)
        {
            Url = url;
            Service = service;

            var exceptionList = new List<string>() { exception.Message };
            if (exception.StackTrace != null) exceptionList.AddRange(exception.StackTrace[6..].Split("\r\n   at "));
            Exception = exceptionList.ToArray();

            if(exception.InnerException != null)
            {
                var innerExceptionList = new List<string>() { exception.InnerException.Message };
                if (exception.InnerException.StackTrace != null) innerExceptionList.AddRange(exception.InnerException.StackTrace[6..].Split("\r\n   at "));
                InnerException = innerExceptionList.ToArray();
            }
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
