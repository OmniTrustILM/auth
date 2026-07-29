using System.Net;

namespace Auth.Common.Exceptions
{
    public class InvalidFormatException : RequestException
    {
        public InvalidFormatException(string message, Exception? innerException = null)
            : base(HttpStatusCode.BadRequest, "INVALID_FORMAT", message, innerException)
        {
        }
    }
}
