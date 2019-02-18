using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Utilities.Networking.Exceptions;

namespace Utilities.Networking
{
    public class OneToOneNetworkConnection : NetworkConnection
    {
        public event OnDataSentEventHandler OnDataSent;
        public event OnDataRecievedEventHandler OnDataRecieved;
        public event OnServerClosedEventHandler OnServerClosed;
        
        public bool Listening
        {
            get
            {
                return serverClosed;
            }
        }
        public long RecieveBufferSize { get; set; }
        private bool replyWaitStarted = false;
        private BackgroundWorker serverThread;
        TcpClient client = new TcpClient();
        private NetworkStream serverStream;
        private TcpClient clientSocket;
        private byte[] replyBuffer;

        public OneToOneNetworkConnection() : base()
        {
            serverThread = new BackgroundWorker();
            serverThread.WorkerReportsProgress = true;
            serverThread.WorkerSupportsCancellation = true;

            serverThread.DoWork += ServerThread_DoWork;
            serverThread.ProgressChanged += ServerThread_ProgressChanged;
            serverThread.RunWorkerCompleted += ServerThread_RunWorkerCompleted;
            this.OnServerClosed += OneToOneNetworkConnection_OnServerClosed;
        }

        public OneToOneNetworkConnection(int port) : this()
        {
            m_port = port;
            this.RecieveBufferSize = 1048576;
        }
        public OneToOneNetworkConnection(string ip, int port) : this(port)
        {
            this.IP = ip;
        }
        public OneToOneNetworkConnection(string ip, int port, long recieveBufferSize) : this()
        {
            m_port = port;
            this.IP = ip;
            this.RecieveBufferSize = recieveBufferSize;
        }

        private void OneToOneNetworkConnection_OnServerClosed(object sender, ServerClosedEventArgs e)
        {
            Connected = false;
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
            TcpListener serverSocket = new TcpListener(System.Net.IPAddress.Any, m_port);
            serverClosed = false;
            new Thread(() =>
            {
                clientSocket = default(TcpClient);
                try
                {
                    serverSocket.Start();
                }
                catch (Exception)
                {
                    throw;
                }
                clientSocket = serverSocket.AcceptTcpClient();
                Connected = true;
                serverThread.RunWorkerAsync(new ServerStartInfo(serverSocket, clientSocket));
            }).Start();
        }

        public override void StopListening()
        {
            serverThread.CancelAsync();
            OnServerClosed(this, new ServerClosedEventArgs("Server closed", ServerState.Canceled, null));
            serverClosed = true;
        }

        public override void StopListening(string reason)
        {
            serverThread.CancelAsync();
            OnServerClosed(this, new ServerClosedEventArgs(reason, ServerState.Canceled, null));
            serverClosed = true;
        }

        public void Connect(int port)
        {
            m_port = port;
            Connect();
        }

        public void Connect()
        {
            if (!client.Connected)
                client.Connect(IP, m_port);
            Connected = true;
        }

        public override void Send(byte[] data)
        {
            try
            {
                client.SendTimeout = 100;
                if (!client.Connected)
                    client.Connect(IP, m_port);
                serverStream = client.GetStream();
            }
            catch (Exception ex)
            {
                throw new ConnectionErrorException("Could not connect to server, check if the device with ip " + IP + " is connected and the program is allowed through firewall", ex);
            }

            try
            {
                serverStream.WriteTimeout = 200;
                byte[] bytes = BitConverter.GetBytes(data.Length);
                System.Diagnostics.Debug.WriteLine(data.Length);
                serverStream.Write(new byte[] { 0xAA, 0xFF, bytes[0], bytes[1], bytes[2], bytes[3] }, 0, sizeof(int) + 2);
                byte[] response = new byte[2];
                serverStream.Read(response, 0, 2);
                if (response[0] == 'o' && response[1] == 'k')
                {
                    serverStream.Write(data, 0, data.Length);
                    Connected = true;
                    OnDataSent?.Invoke(this, new DataSentEventArgs(data, StringTerminator));
                    if (!replyWaitStarted) //TODO: Do something CPU usage of this
                    {
                        //ThreadPool.QueueUserWorkItem(WaitForReply, serverStream);
                        //replyWaitStarted = true;
                    }
                }
                else
                {
                    throw new CommunicationException("Invalid response recieved from server");
                }
            }
            catch (System.IO.IOException ex)
            {
                serverStream.Close();
                serverStream.Dispose();
                client.Close();
                Connected = false;
                throw new Exception("Server was closed", ex);
            }
        }

        public override void Send(string text)
        {
            Send(Encoding.ASCII.GetBytes(text + StringTerminator));
        }

        public void Reply(byte[] data)
        {
            if (serverClosed)
                throw new InvalidOperationException("The server is not open");
            NetworkStream stream = clientSocket.GetStream();

            stream.Write(BitConverter.GetBytes(data.Length), 0, sizeof(int));
            replyBuffer = data;


            OnDataSent?.Invoke(this, new DataSentEventArgs(data, StringTerminator));
        }

        public void Reply(string text)
        {
            Reply(Encoding.ASCII.GetBytes(text + StringTerminator));
        }

        private void WaitForReply(object data)
        {
            while (true)
            {
                try
                {
                    NetworkStream serverStream = data as NetworkStream;
                    byte[] bytesToRecieve = new byte[sizeof(int)];

                    serverStream.Read(bytesToRecieve, 0, sizeof(int));
                    serverStream.Write(new byte[] { (byte)'o', (byte)'k' }, 0, 2);

                    int byt = BitConverter.ToInt32(bytesToRecieve, 0);
                    byte[] inStream = new byte[byt];
                    serverStream.Read(inStream, 0, byt);

                    OnDataRecieved?.Invoke(this, new DataRecievedEventArgs(inStream, client.ReceiveBufferSize,(byte)StringTerminator));
                    // WARNING : cross thread errors may occur from here
                }
                catch
                { }
            }            
        }

        private void ServerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                OnServerClosed?.Invoke(this, new ServerClosedEventArgs("An unhandled exception has occured", ServerState.Error, e.Error.InnerException));
            else if (e.Cancelled)
            {
                if (!serverClosed)
                    OnServerClosed?.Invoke(this, new ServerClosedEventArgs("An error occured", ServerState.Error, null));
            }
            else
            {
                ServerClosedResult info = e.Result as ServerClosedResult;
                OnServerClosed?.Invoke(this, new ServerClosedEventArgs(info.Message, ServerState.Done, info.InnerException));
            }
            serverClosed = true;
        }

        private void ServerThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ServerThreadState state = e.UserState as ServerThreadState;
            OnDataRecieved?.Invoke(this, new DataRecievedEventArgs(state.Data.Clone() as byte[], state.Length, state.RecievedTime, (byte)StringTerminator));
        }

        private void ServerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            ServerStartInfo info = e.Argument as ServerStartInfo;
            int requests = 0;

            if (serverThread.CancellationPending)
            {
                e.Cancel = true;
                e.Result = new ServerClosedResult("Canceled", info);
                return;
            }

            while (true)
            {
                if (serverThread.CancellationPending)
                {
                    e.Cancel = true;
                    e.Result = new ServerClosedResult("Canceled", info);
                    return;
                }
                try
                {
                    NetworkStream networkStream = info.ClientSocket.GetStream();
                    byte[] bytesToRecieve = new byte[sizeof(int) + 2];

                    if (serverThread.CancellationPending)
                    {
                        e.Cancel = true;
                        e.Result = new ServerClosedResult("Canceled", info);
                        return;
                    }
                    networkStream.Read(bytesToRecieve, 0, sizeof(int) + 2);

                    //That mean this comes from reply
                    if (bytesToRecieve[2] == 'o' && bytesToRecieve[3] == 'k' && bytesToRecieve[4] == 0)
                    {
                        if (replyBuffer != null)
                        {
                            networkStream.Write(replyBuffer, 0, replyBuffer.Length);
                            replyBuffer = null;
                        }
                        else
                        {
                            #pragma warning disable
                            throw new ExecutionEngineException("An error while getting the reply buffer. This may be due to the sending of 'ok' over the network when a message length was expected");
                            #pragma warning restore
                        }
                    }
                    else
                    {
                        if (bytesToRecieve[0] == 0)
                            continue;
                        networkStream.Write(new byte[] { (byte)'o', (byte)'k' }, 0, 2);

                        int byt = BitConverter.ToInt32(new byte[] { bytesToRecieve[2], bytesToRecieve[3], bytesToRecieve[4], bytesToRecieve[5] } , 0);
                        System.Diagnostics.Debug.WriteLine(byt);
                        byte[] bytesFrom = new byte[byt];
                        networkStream.Read(bytesFrom, 0, byt);

                        if (serverThread.CancellationPending)
                        {
                            e.Cancel = true;
                            e.Result = new ServerClosedResult("Canceled", info);
                            return;
                        }
                        serverThread.ReportProgress(requests++, new ServerThreadState(bytesFrom, info.ClientSocket.ReceiveBufferSize, ServerState.Running));

                        if (serverThread.CancellationPending)
                        {
                            e.Cancel = true;
                            e.Result = new ServerClosedResult("Canceled", info);
                            return;
                        }
                    }
                    networkStream.Flush();
                }
                catch (Exception ex)
                {
                    info.ClientSocket.Close();
                    info.ServerSocket.Stop();
                    e.Result = new ServerClosedResult("An exception occured in the server thread", ex, info);

                    e.Cancel = true;
                    return;
                }
            }
        }

        public new void Dispose()
        {
            serverThread.CancelAsync();
            serverThread.Dispose();
        }
    }
}
