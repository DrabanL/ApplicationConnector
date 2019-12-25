using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;

namespace RabanSoft.ApplicationConnector.ConnectorHandlers
{
    public class PipelineConnectorClient : ConnectorBase
    {
        private readonly string _basePipeName;
        private readonly string _host;

        internal override IEnumerable<Stream> GetConnectedStreams() => _streams.Where(stream => (stream as NamedPipeClientStream).IsConnected);

        public PipelineConnectorClient(string basePipeName, string host = ".") : base()
        {
            _host = host;
            _basePipeName = basePipeName;
        }

        public PipelineConnectorClient(string serverProcessName) : base()
        {
            _host = ".";
            _basePipeName = getDefaultPipeName(serverProcessName);
        }

        private string getDefaultPipeName(string processName) => $"{Process.GetProcessesByName(processName).First().Id}";

        public override async void Start()
        {
            if (_streams.Count > 0)
                return;

            var stream = new NamedPipeClientStream(_host, $"{_basePipeName}-connector-iostream", PipeDirection.InOut, PipeOptions.Asynchronous);
            _streams.Add(stream);

            try
            {
                await stream.ConnectAsync().ConfigureAwait(false);
                await receiveAsync(stream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }
    }
}
