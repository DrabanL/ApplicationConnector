using RabanSoft.ApplicationConnector.ConnectorHandlers;
using System;
using System.IO;

namespace RabanSoft.ApplicationConnector.IOBinders
{
    public static class ConsoleIOBinder
    {
        public static event Action<(byte[] data, int offset, int count)> OnData;
        public static event Action<Exception> OnError;

        private static ConnectorBase _connectorConsumer;
        private static ConsoleMinimalStream _stream;
        private static StreamWriter _streamWriter;
        private static TextWriter _defaultStream;
        
        public static void Bind(ConnectorBase consumer = null)
        {
            _connectorConsumer = consumer;

            if (_streamWriter == null)
            {
                _stream = new ConsoleMinimalStream();
                _stream.OnData += Stream_OnData;
                _streamWriter = new StreamWriter(_stream) { AutoFlush = true };
            }

            _defaultStream = Console.Out;
            Console.SetOut(_streamWriter);
        }

        private static void Stream_OnData((byte[] buffer, int offset, int count) obj)
        {
            _connectorConsumer?.Send(obj.buffer, obj.offset, obj.count);

            try
            {
                OnData?.Invoke(obj);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        public static void UnBind()
        {
            if (_defaultStream != null)
                Console.SetOut(_defaultStream);

            if (_stream != null)
                _stream.OnData -= Stream_OnData;
            _stream?.Dispose();
            _streamWriter?.Dispose();
            _streamWriter = null;
        }
    }
}
