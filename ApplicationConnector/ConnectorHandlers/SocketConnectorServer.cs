using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RabanSoft.ApplicationConnector.ConnectorHandlers
{
    public class SocketConnectorServer : ConnectorBase
    {
        private int _port;
        private int _backlogCount;
        private Socket _socket;

        internal override IEnumerable<Stream> GetConnectedStreams() => _streams;

        public SocketConnectorServer(int port = 42535, int backlog = 1000) : base()
        {
            _port = port;
            _backlogCount = backlog;
        }

        public override void Start() {
            if (_socket != null)
                return;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
            _socket.Listen(_backlogCount);

            doAccept();
        }

        private async void doAccept()
        {
            while (true)
            {
                Socket socket;

                try
                {
                    socket = await _socket.AcceptAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                        OnError?.Invoke(ex);

                    break;
                }

                processSocket(new NetworkStream(socket, true));
            }
        }

        private async void processSocket(NetworkStream networkStream)
        {
            using (networkStream)
            {
                _streams.Add(networkStream);

                try
                {
                    await receiveAsync(networkStream).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }

                _streams.Remove(networkStream);
            }
        }

        public override void Stop()
        {
            _socket?.Close();
            _socket?.Dispose();
            _socket = null;

            base.Stop();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }

            base.Dispose(disposing);
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PipelineConnectorServer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);

            base.Dispose(true);
        }
        #endregion
    }
}
