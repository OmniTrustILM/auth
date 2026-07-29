using System.Net;

namespace Auth.Common.Exceptions
{
    public class UnauthorizedException : RequestException
    {
        public UnauthorizedException(string message, Exception? innerException = null)
            : base(HttpStatusCode.Unauthorized, "UNAUTHORIZED", message, innerException)
        {
        }
    }
}
