/// From https://stackoverflow.com/questions/4672010/multi-threading-with-net-httplistener


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utilities.Networking
{
    public class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;
        private readonly ServerContainer _parent;

        public HttpServer(ServerContainer parent, int maxThreads)
        {
            _parent = parent;
            _workers = new Thread[maxThreads];
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        public void Start(int port)
        {
            _listener.Prefixes.Add(String.Format(@"http://+:{0}/", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Dispose()
        { Stop(); }

        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try { HttpServer_ProcessRequest(context); }
                catch (Exception e) { Console.Error.WriteLine(e); }
            }
        }

        private void HttpServer_ProcessRequest(System.Net.HttpListenerContext context)
        {
            var data_text = new StreamReader(context.Request.InputStream,
            context.Request.ContentEncoding).ReadToEnd();

            //functions used to decode json encoded data.
            var data1 = Uri.UnescapeDataString(data_text);
            string da = Regex.Unescape(data_text);

            var cleaned_data = WebUtility.UrlDecode(data_text);

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";

            string[] pairs = cleaned_data.Split(new char[] { '&' });
            Dictionary<string, string> valuePairs = new Dictionary<string, string>(10);
            foreach (var pair in pairs)
            {
                var values = pair.Split(new char[] { '=' });
                valuePairs.Add(values[0], values[1]);
            }

            context.Response.Headers.Clear();
            context.Response.SendChunked = false;
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Server", String.Empty);
            context.Response.Headers.Add("Date", String.Empty);
            context.Response.Close();

            ProcessRequestCT(valuePairs);
        }

        delegate void ProcessRequestCallback(Dictionary<string, string> valuePairs);
        private void ProcessRequestCT(Dictionary<string, string> valuePairs)
        {
            if (_parent.InvokeRequired)
            {
                ProcessRequestCallback d = new ProcessRequestCallback(ProcessRequestCT);
                try
                {
                    _parent.Invoke(d, new object[] { valuePairs });
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                _parent.ProcessRequest(valuePairs);
            }
        }
    }
}
