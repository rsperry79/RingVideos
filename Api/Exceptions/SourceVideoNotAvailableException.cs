using System;

namespace KoenZomers.Ring.Api.Exceptions
{
    /// <summary>
    /// Exception thrown when the Ring API explicitly reports the source video is not available
    /// </summary>
    public class SourceVideoNotAvailableException : Exception
    {
        private const string errorMessage = "Source video not available for '{0}'";
        private const string errorMessageWithDetails = "Source video not available for '{0}'. Server response: {1}";

        public SourceVideoNotAvailableException(string idOrContext) : base(string.Format(errorMessage, string.IsNullOrEmpty(idOrContext) ? "unknown" : idOrContext))
        {
        }

        public SourceVideoNotAvailableException(string idOrContext, string serverResponse) : base(string.Format(errorMessageWithDetails, string.IsNullOrEmpty(idOrContext) ? "unknown" : idOrContext, serverResponse))
        {
        }

        public SourceVideoNotAvailableException(string idOrContext, Exception inner) : base(string.Format(errorMessage, string.IsNullOrEmpty(idOrContext) ? "unknown" : idOrContext), inner)
        {
        }
    }
}
