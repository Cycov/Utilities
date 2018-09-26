using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
        private bool replyWaitStarted = false;
        private BackgroundWorker serverThread;
        TcpClient client = new TcpClient();
        private NetworkStream serverStream;
        private TcpClient clientSocket;

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

        private void OneToOneNetworkConnection_OnServerClosed(object sender, ServerClosedEventArgs e)
        {
            Connected = false;
        }

        public OneToOneNetworkConnection(int port) : this()
        {
            m_port = port;
        }

        public OneToOneNetworkConnection(string ip, int port) : this(port)
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
            TcpListener serverSocket = new TcpListener(System.Net.IPAddress.Any, m_port);
            serverClosed = false;
            //this blocks the thread, server thread should run in a separate thread
            // WARNING : run serverThread in another bgworker and call OnDataRecieved from there
                clientSocket = default(TcpClient);
                serverSocket.Start();
                clientSocket = serverSocket.AcceptTcpClient();
                Connected = true;
                serverThread.RunWorkerAsync(new ServerStartInfo(serverSocket, clientSocket));
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
                throw new Exception("Could not connect to server, check if the device with ip " + IP + " is connected and the program is allowed through firewall", ex);
            }
            try
            {
                serverStream.WriteTimeout = 200;
                serverStream.Write(data, 0, data.Length);
                Connected = true;
                OnDataSent?.Invoke(this, new DataSentEventArgs(data, StringTerminator));
                if (!replyWaitStarted) //TODO: Do something CPU usage of this
                {
                    ThreadPool.QueueUserWorkItem(WaitForReply, serverStream);
                    replyWaitStarted = true;
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
            stream.Write(data, 0, data.Length);
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
                    byte[] inStream = new byte[1048576];
                    serverStream.Read(inStream, 0, client.ReceiveBufferSize);
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
                    byte[] bytesFrom = new byte[1048576];

                    if (serverThread.CancellationPending)
                    {
                        e.Cancel = true;
                        e.Result = new ServerClosedResult("Canceled", info);
                        return;
                    }
                    networkStream.Read(bytesFrom, 0, (int)info.ClientSocket.ReceiveBufferSize);

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
