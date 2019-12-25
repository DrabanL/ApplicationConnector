using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace RabanSoft.ApplicationConnector.ConnectorHandlers
{
    public class PipelineConnectorServer : ConnectorBase
    {
        private readonly int _pipeLimit;
        private readonly string _basePipeName;

        internal override IEnumerable<Stream> GetConnectedStreams() => _streams.Cast<NamedPipeServerStream>().Where(stream => stream.IsConnected);

        public PipelineConnectorServer(int pipeLimit = 30, string basePipeName = null) : base()
        {
            _basePipeName = basePipeName ?? getBasePipeName();
            _pipeLimit = pipeLimit;
        }

        private string getBasePipeName() => $"{Process.GetCurrentProcess().Id}";

        public override void Start()
        {
            if (_streams.Count > 0)
                return;

            for (int i = 0; i < _pipeLimit; ++i)
                remakeStream();
        }

        private void remakeStream(NamedPipeServerStream stream = null)
        {
            stream?.Dispose();

            _streams.Remove(stream);
            _streams.Add(stream = new NamedPipeServerStream($"{_basePipeName}-connector-iostream", PipeDirection.InOut, _pipeLimit, PipeTransmissionMode.Byte, PipeOptions.Asynchronous));

            processStream(stream).ContinueWith(task =>
            {
                if (task.Result)
                    remakeStream(stream);
            }).ConfigureAwait(false);
        }

        private async Task<bool> processStream(NamedPipeServerStream stream)
        {
            try
            {
                await stream.WaitForConnectionAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    OnError?.Invoke(ex);

                return !_cancellationTokenSource.IsCancellationRequested;
            }

            try
            {
                return await receiveAsync(stream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }

            return true;
        }
    }
}
