using System;
using System.Text;

namespace Utilities.Networking
{
    public delegate void OnDataRecievedEventHandler(object sender, DataRecievedEventArgs e);
    public delegate void OnServerClosedEventHandler(object sender, ServerClosedEventArgs e);
    
    public delegate void OnDataSentEventHandler(object sender, DataSentEventArgs e);
    public delegate void OnListenStopEventHandler(object sender, ListenStopEventArgs e);
    public delegate void OnListenStartEventHandler(object sender, ListenStartEventArgs e);

    public class DataRecievedEventArgs : Events.EmptyEventArgs<DataRecievedEventArgs>
    {
        public byte[] Data
        {
            get;
            protected set;
        }
        public string Text
        {
            get
            {
                string dataFromClient = Encoding.ASCII.GetString(Data);
                int indexOfTerminator = dataFromClient.IndexOf((char)EndByte);
                return dataFromClient.Substring(0, indexOfTerminator);
            }
        }
        public DateTime RecievedTime
        {
            get;
            protected set;
        }

        public int Length
        {
            get;
            protected set;
        }

        public byte EndByte
        {
            get;
            protected set;
        }

        public char Terminator
        {
            get
            {
                return (char)EndByte;
            }
        }

        public DataRecievedEventArgs(byte[] data,int length, DateTime recievedTime, byte endByte)
        {
            this.Data = data;
            RecievedTime = recievedTime;
            this.EndByte = endByte;
        }
        public DataRecievedEventArgs(byte[] data, int length, byte endByte)
        {
            this.Data = data;
            RecievedTime = DateTime.Now;
            this.EndByte = endByte;
        }
    }

    public class ServerClosedEventArgs : ListenStopEventArgs
    {
        public ServerState State
        {
            get;
            protected set;
        }

        public ServerClosedEventArgs(string message, ServerState state, Exception exception) : base(message, exception)
        {
            Message = message;
            InnerException = exception;
            State = state;
        }
    }

    public class ListenStopEventArgs : ListenStartEventArgs
    {        
        public Exception InnerException
        {
            get;
            protected set;
        }
        public ListenStopEventArgs(string message, Exception exception) : base(message)
        {
            Message = message;
            InnerException = exception;
        }
        public ListenStopEventArgs(string message) : base(message)
        {
            Message = message;
            InnerException = null;
        }
    }

    public class ListenStartEventArgs : Events.EmptyEventArgs<ListenStartEventArgs>
    {
        public string Message
        {
            get;
            protected set;
        }

        public ListenStartEventArgs(string message)
        {
            Message = message;
        }

        public ListenStartEventArgs()
        {
            Message = String.Empty;
        }
    }

    public class DataSentEventArgs : ListenStartEventArgs
    {
        public byte[] Data
        {
            get;
            protected set;
        }

        private char terminator;

        public DataSentEventArgs(byte[] data, char terminator)
        {
            Data = data;
            this.terminator = terminator;

            string text = Encoding.ASCII.GetString(Data);
            Message = text.Substring(0, text.IndexOf(terminator));            
        }
    }
}
