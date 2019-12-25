#define SOCKET_IO
#undef SOCKET_IO
#define PIPES_IO
//#undef PIPES_IO
#define XOR_ENC
#undef XOR_ENC
#define DES_ENC
//#undef DES_ENC
#define ATTACH_IO
//#undef ATTACH_IO

using RabanSoft.ApplicationConnector.ConnectorHandlers;
using RabanSoft.ApplicationConnector.DataTransformers;
using RabanSoft.ApplicationConnector.IOBinders;
using System;
using System.IO;
using System.Text;

namespace Example.Client
{
    class Program
    {
#if DES_ENC
        class CustomizedCryptoTransformer : TripleDESCryptoDataTransformer
        {
            public override string SecretKey { get; set; } = "Xsrce8vSf5jY72nk";
        }
#endif
#if XOR_ENC
        class CustomizedCryptoTransformer : XorDataTransformer
        {
            public override byte[] SecretKey { get; set; } = new byte[] { 0x0B, 0xFF, 0xDE };
        }
#endif
#if PIPES_IO
        class CustomizedConnectorClient : PipelineConnectorClient
        {
            public override IDataTransformer DataTransformer { get; set; } = new CustomizedCryptoTransformer();

            public CustomizedConnectorClient(string serverProcessName) : base(serverProcessName) { }
        }
#endif
#if SOCKET_IO
        class CustomizedConnectorClient : SocketConnectorClient
        {
            public override IDataTransformer DataTransformer { get; set; } = new CustomizedCryptoTransformer();
        }
#endif
        static void Main(string[] args)
        {
#if ATTACH_IO
            ProcessIOBinder.OnData += onDebugData;
            ProcessIOBinder.OnError += ProcessIOBinder_OnError;
            ProcessIOBinder.Attach("Example.Server");
#endif
            var connectorClient = new CustomizedConnectorClient(
#if PIPES_IO
                "Example.Server"
#endif
                );
            connectorClient.OnError += ConnectorClient_OnError;
            connectorClient.OnDataReceived += connectorServerDataReceived;
            connectorClient.Start();

            Console.CancelKeyPress += (s, e) => e.Cancel = true;

            while (Console.ReadLine() is var command && command != null)
                connectorClient.Send(Encoding.ASCII.GetBytes(command));

#if ATTACH_IO
            ProcessIOBinder.DetachAll();
#endif
            connectorClient.Stop();

            Console.WriteLine("stopped");
            Console.ReadKey(true);
        }

        private static void ConnectorClient_OnError(Exception obj) => Console.WriteLine($"[{nameof(ConnectorClient_OnError)}] {obj}");
        private static void ProcessIOBinder_OnError(Exception obj) => Console.WriteLine($"[{nameof(ProcessIOBinder_OnError)}] {obj}");
        private static void onDebugData((string process, string message) obj) => Console.WriteLine($"rec: ({obj.process}) {obj.message}");

        private static void connectorServerDataReceived(ConnectorBase connector, Stream stream, byte[] obj) => Console.WriteLine($"rec: {Encoding.ASCII.GetString(obj)}");
    }
}
