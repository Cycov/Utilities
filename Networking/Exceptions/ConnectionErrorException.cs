using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Networking.Exceptions
{

    [Serializable]
    public class ConnectionErrorException : Exception
    {
        public ConnectionErrorException() { }
        public ConnectionErrorException(string message) : base(message) { }
        public ConnectionErrorException(string message, Exception inner) : base(message, inner) { }
        protected ConnectionErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
