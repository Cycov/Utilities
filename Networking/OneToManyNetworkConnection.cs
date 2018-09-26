using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;

namespace Utilities.Networking
{
    public class OneToManyNetworkConnection : NetworkConnection
    {
        public event OnDataSentEventHandler OnDataSent;
        public event OnDataRecievedEventHandler OnDataRecieved;
        public event OnListenStartEventHandler OnListenStart;
        public event OnListenStopEventHandler OnListenStop;
        
        /// <summary>
        /// The maximum number of clients that can connect to the server
        ///  Defaults to Int32.MaxValue
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Description("The maximum number of clients that can connect to the server"), Category("Connection info")]
        public int MaximumIncomingConnections
        {
            get;
            set;
        } = Int32.MaxValue;
        
        private TcpClient client = new TcpClient();
        private List<TcpClient> connectedClients;

        public OneToManyNetworkConnection() : base() { }

        public OneToManyNetworkConnection(int port) : this()
        {
            m_port = port;
            connectedClients = new List<TcpClient>();
        }

        public OneToManyNetworkConnection(string ip, int port) : this(port)
        {
            this.IP = ip;
        }


        public override void StartListening(int port)
        {
            m_port = port;
            StartListening();
        }

        /// <summary>
        /// Allow data to be read from clients
        /// </summary>
        public override void StartListening()
        {
            try
            {
                serverClosed = false;
                TcpListener serverSocket = new TcpListener(IPAddress.Any, m_port);
                TcpClient clientSocket = default(TcpClient);
                serverSocket.Start();
                new Thread(() =>
                {
                    try
                    {
                        for (int i = 0; i < MaximumIncomingConnections && !serverClosed; i++)
                        {
                            clientSocket = serverSocket.AcceptTcpClient();
                            ThreadPool.QueueUserWorkItem(ServerThread, new ServerStartInfo(null, clientSocket));
                            connectedClients.Add(clientSocket);

                        }
                    }
                    catch (Exception ex)
                    {
                        OnListenStop?.Invoke(this, new ListenStopEventArgs("An error occures while trying to accept incoming data", ex));
                    }
                }).Start();
                OnListenStart?.Invoke(this, ListenStartEventArgs.Empty);

            }
            catch (SocketException ex)
            {
                throw new Exception("Already listening. If you wish to listen on another port instantiate another object", ex);
            }
        }

        public override void StopListening()
        {
            serverClosed = true;
            OnListenStop?.Invoke(this, new ListenStopEventArgs("Canceled by user"));
        }

        public override void StopListening(string reason)
        {
            serverClosed = true;
            OnListenStop?.Invoke(this, new ListenStopEventArgs(reason));
        }

        private void ServerThread(object data)
        {
            ServerStartInfo info = data as ServerStartInfo;
            NetworkStream networkStream = info.ClientSocket.GetStream();

            while (!serverClosed)
            {
                try
                {
                    byte[] bytesFrom = new byte[1048576];
                    networkStream.Read(bytesFrom, 0, info.ClientSocket.ReceiveBufferSize);
                    if (bytesFrom.Length > 0)
                        OnDataRecieved?.Invoke(this, new DataRecievedEventArgs(bytesFrom, info.ClientSocket.ReceiveBufferSize,(byte)StringTerminator));
                }
                catch (System.IO.IOException)
                {
                    info.ClientSocket.Close();
                    return;
                }
                catch (Exception ex)
                {
                    info.ClientSocket.Close();
                    StopListening(ex.Message + Environment.NewLine + ex.StackTrace);
                    return;
                }
            }
        }

        /// <summary>
        /// Sends a ASCII message to all connected clients
        /// </summary>
        /// <param name="message">The message to be sent</param>
        public void Broadcast(string message)
        {
                foreach (TcpClient client in connectedClients)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        Byte[] sendBytes = Encoding.ASCII.GetBytes(message);
                        stream.Write(sendBytes, 0, sendBytes.Length);
                    }
                    catch
                    { }
                }         
        }

        public override void Send(byte[] data)
        {
            try
            {
                if (!client.Connected)
                    client.Connect(IP, 8888);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not connect to server, check if the device with ip " + IP + " is connected and the program is allowed through firewall", ex);
            }
            NetworkStream serverStream = client.GetStream();
            try
            {
                serverStream.Write(data, 0, data.Length);
                serverStream.Flush();
                OnDataSent(this, new DataSentEventArgs(data, StringTerminator));
            }
            catch (System.IO.IOException ex)
            {
                serverStream.Close();
                throw new Exception("Server was closed", ex);
            }
        }

        public override void Send(string text)
        {
            Send(Encoding.ASCII.GetBytes(text + StringTerminator));
        }
    }
}
