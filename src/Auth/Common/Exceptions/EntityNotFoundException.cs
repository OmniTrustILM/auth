using System.Net;

namespace Auth.Common.Exceptions
{
    public class EntityNotFoundException : RequestException
    {
        public EntityNotFoundException(string message, Exception? innerException = null)
            : base(HttpStatusCode.NotFound, "ENTITY_NOT_FOUND", message, innerException)
        {
        }
    }
}
