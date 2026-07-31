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
            exceptionList.AddRange(Frames(exception.StackTrace));
            Exception = exceptionList.ToArray();

            if(exception.InnerException != null)
            {
                var innerExceptionList = new List<string>() { exception.InnerException.Message };
                innerExceptionList.AddRange(Frames(exception.InnerException.StackTrace));
                InnerException = innerExceptionList.ToArray();
            }
        }

        /// <summary>
        /// One entry per stack frame, with the "at " prefix dropped. Frames are separated by the line ending of the
        /// machine that produced the trace, so both are accepted: the service runs on Linux, while a trace captured
        /// during a Windows development run carries carriage returns.
        /// </summary>
        private static IEnumerable<string> Frames(string? stackTrace)
        {
            if (stackTrace == null) return [];

            return stackTrace
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(frame => frame.StartsWith("at ", StringComparison.Ordinal) ? frame[3..] : frame);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
