using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RabanSoft.ApplicationConnector.ConnectorHandlers
{
    public class SocketConnectorClient : ConnectorBase
    {
        private int _port;
        private IPAddress _address;

        internal override IEnumerable<Stream> GetConnectedStreams() => _streams;

        public SocketConnectorClient(IPAddress address = null, int port = 42535) : base()
        {
            _address = address ?? IPAddress.Loopback;
            _port = port;
        }

        public override async void Start() {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(new IPEndPoint(_address, _port)).ConfigureAwait(false);

            processSocket(new NetworkStream(socket, true));
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
    }
}
