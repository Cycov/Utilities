using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Networking.Exceptions
{

    [Serializable]
    public class HttpServerAccessDeniedExceptionException : Exception
    {
        public HttpServerAccessDeniedExceptionException() { }
        public HttpServerAccessDeniedExceptionException(string message) : base(message) { }
        public HttpServerAccessDeniedExceptionException(string message, Exception inner) : base(message, inner) { }
        protected HttpServerAccessDeniedExceptionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
