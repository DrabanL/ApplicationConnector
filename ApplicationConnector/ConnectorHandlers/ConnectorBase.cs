using RabanSoft.ApplicationConnector.DataTransformers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RabanSoft.ApplicationConnector.ConnectorHandlers
{
    public abstract class ConnectorBase : IDisposable
    {
        public virtual IDataTransformer DataTransformer { get; set; } = new DefaultDataTransformer();
        public event Action<ConnectorBase, Stream, byte[]> OnDataReceived;
        public Action<Exception> OnError;
        private readonly SemaphoreSlim _writeLocker = new SemaphoreSlim(1, 1);

        public virtual int MaxDataSize { get; set; } = 0x8096;
        public virtual int DataBufferSize { get; set; } = 0x128;

        internal readonly CancellationTokenSource _cancellationTokenSource;
        internal readonly List<Stream> _streams;
        internal abstract IEnumerable<Stream> GetConnectedStreams();

        public ConnectorBase()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _streams = new List<Stream>();
        }

        public void Send(byte[] data) => Send(data, 0, data.Length);

        public void Send(byte[] data, int offset, int count)
        {
            try
            {
                data = DataTransformer.TransformOut(data, offset, count);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return;
            }

            foreach (var stream in GetConnectedStreams())
                send(stream, data);
        }

        public void Send(Stream stream, byte[] data)
        {
            try
            {
                data = DataTransformer.TransformOut(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return;
            }

            send(stream, data);
        }

        private async void send(Stream stream, byte[] data)
        {
            await _writeLocker.WaitAsync(_cancellationTokenSource.Token);
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            try
            {
                await stream.WriteAsync(BitConverter.GetBytes(data.Length), 0, 4, _cancellationTokenSource.Token).ConfigureAwait(false);
                await stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token).ConfigureAwait(false);
                await stream.FlushAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    OnError?.Invoke(ex);
            }
            finally
            {
                _writeLocker.Release();
            }
        }

        public abstract void Start();

        public virtual void Stop()
        {
            _cancellationTokenSource.Cancel(false);


            foreach (var stream in _streams)
                try
                {
                    using (stream)
                        stream.Close();
                }
                catch { }

            _streams.Clear();
        }

        internal async Task<bool> receiveAsync(Stream stream)
        {
            var dataBuffer = new byte[DataBufferSize];
            int offset;
            int rem;
            int len;

            while (true)
            {
                offset = 0;
                len = 0;

                while (offset < 4)
                {
                    try
                    {
                        offset += (len = await stream.ReadAsync(dataBuffer, offset, 4 - offset, _cancellationTokenSource.Token).ConfigureAwait(false));
                    }
                    catch (Exception ex)
                    {
                        if (!_cancellationTokenSource.IsCancellationRequested)
                            OnError?.Invoke(ex);

                        return !_cancellationTokenSource.IsCancellationRequested;
                    }

                    if (len == 0)
                        return true;
                }

                var dataSize = BitConverter.ToInt32(dataBuffer, 0);
                if (dataSize < 0)
                {
                    OnError?.Invoke(new Exception($"dataSize < 0"));
                    return true;
                }

                if (dataSize > MaxDataSize)
                {
                    OnError?.Invoke(new Exception($"dataSize > MaxDataSize"));
                    return true;
                }

                rem = dataSize;
                do
                {
                    var readCnt = rem < DataBufferSize ? rem : DataBufferSize;

                    if (offset + readCnt > dataBuffer.Length)
                        Array.Resize(ref dataBuffer, offset + readCnt);

                    try
                    {
                        offset += (len = await stream.ReadAsync(dataBuffer, offset, readCnt, _cancellationTokenSource.Token).ConfigureAwait(false));
                    }
                    catch (Exception ex)
                    {
                        if (!_cancellationTokenSource.IsCancellationRequested)
                            OnError?.Invoke(ex);

                        return !_cancellationTokenSource.IsCancellationRequested;
                    }

                    if (len == 0)
                        // other endpoint disconnected gracefully
                        return true;

                    rem -= len;
                } while (rem > 0);

                try
                {
                    var transformedData = DataTransformer.TransformIn(dataBuffer, 4, dataSize);

                    try
                    {
                        OnDataReceived?.Invoke(this, stream, transformedData);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(ex);
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }

                Array.Resize(ref dataBuffer, DataBufferSize);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    (DataTransformer as IDisposable)?.Dispose();
                    _cancellationTokenSource?.Cancel(false);
                    _cancellationTokenSource?.Dispose();

                    _streams?.ForEach(value => value.Dispose());
                    _streams?.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ConnectorBase()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
