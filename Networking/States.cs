using System;
using System.Net.Sockets;

namespace Utilities.Networking
{
    public enum ServerState
    {
        Running,
        Done,
        Canceled,
        Error
    }
    internal class ServerThreadState
    {
        public byte[] Data
        {
            get;
            protected set;
        }

        public int Length
        {
            get;
            protected set;
        }

        public DateTime RecievedTime
        {
            get;
            protected set;
        }

        public ServerState State
        {
            get;
            protected set;
        }


        public ServerThreadState(byte[] data, int length, ServerState state)
        {
            this.Data = data;
            RecievedTime = DateTime.Now;
            State = state;
            Length = length;
        }
    }

    internal class ServerStartInfo
    {
        public TcpListener ServerSocket
        {
            get;
            protected set;
        }
        public TcpClient ClientSocket
        {
            get;
            protected set;
        }

        public static ServerStartInfo Instance;

        public ServerStartInfo(TcpListener serverSocket, TcpClient clientSocket)
        {
            this.ServerSocket = serverSocket;
            this.ClientSocket = clientSocket;
            Instance = this;
        }
    }

    internal class ServerClosedResult : ServerStartInfo
    {
        public string Message
        {
            get;
            protected set;
        }

        public Exception InnerException
        {
            get;
            protected set;
        }

        public ServerClosedResult(TcpListener serverSocket, TcpClient tcpClient) : base(serverSocket, tcpClient) { }

        public ServerClosedResult(String message, TcpListener serverSocket, TcpClient tcpClient) : this(serverSocket, tcpClient)
        {
            this.Message = message;
        }

        public ServerClosedResult(ServerStartInfo info) : base(info.ServerSocket, info.ClientSocket) { }
        public ServerClosedResult(String message, ServerStartInfo info) : this(info)
        {
            this.Message = message;
        }
        public ServerClosedResult(String message, Exception exception, ServerStartInfo info) : this(info)
        {
            this.Message = message;
            this.InnerException = exception;
        }
    }
}
