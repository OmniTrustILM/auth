using System.Net;

namespace Auth.Common.Exceptions
{
    public class RequestException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string Code { get; private set; }

        public RequestException(HttpStatusCode statusCode, string code, string message, Exception? innerException = null)
            :base(message, innerException)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }
}
