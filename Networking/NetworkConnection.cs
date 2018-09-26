using System.ComponentModel;

namespace Utilities.Networking
{
    public abstract class NetworkConnection : Component
    {
        protected int m_port = 8888;
        protected bool serverClosed = false;
        /// <summary>
        /// Defaults to 127.0.0.1 - localhost
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Description("The ip the connection should send the data to \nDefaults to localhost"), Category("Connection info")]
        public string IP
        {
            get;
            set;
        } = "127.0.0.1";


        /// <summary>
        /// Defaults to 8888
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Description("The port on which the connection should be opened\nOnly one connection per port"), Category("Connection info")]
        public int Port
        {
            get
            {
                return m_port;
            }
            set
            {
                m_port = value;
            }
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Description("The character that marks the end of a string if sending text is intended"), Category("Connection info")]
        public char StringTerminator
        {
            get;
            set;
        } = '$';

        public string LastMessage
        {
            get;
            protected set;
        } = string.Empty;

        [Browsable(false)]
        public bool Connected
        {
            get;
            protected set;
        } = false;

        public NetworkConnection() : base() { }

        public abstract void StartListening();
        public abstract void StartListening(int port);
        public abstract void StopListening();
        public abstract void StopListening(string reason);
        public abstract void Send(byte[] data);
        public abstract void Send(string text);
        public override string ToString()
        {
            return string.Format("Network connection:\n Status: {0}\nIp: {1}\nPort: {2}\n Last message: {3}",
                serverClosed?"Not listening, ready to send":"Listening for incoming connections",
                IP,
                m_port,
                LastMessage);
        }
    }
}
