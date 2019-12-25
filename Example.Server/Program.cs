#define SOCKET_IO
#undef SOCKET_IO
#define PIPES_IO
//#undef PIPES_IO
#define XOR_ENC
#undef XOR_ENC
#define DES_ENC
//#undef DES_ENC

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using RabanSoft.ApplicationConnector.ConnectorHandlers;
using RabanSoft.ApplicationConnector.DataTransformers;
using RabanSoft.ApplicationConnector.IOBinders;

namespace Example.Server
{
    class Program
    {
        static bool abort;

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
        class CustomizedConnectorServer : PipelineConnectorServer
        {
            public override IDataTransformer DataTransformer { get; set; } = new CustomizedCryptoTransformer();
        }
#endif
#if SOCKET_IO
        class CustomizedConnectorServer : SocketConnectorServer
        {
            public override IDataTransformer DataTransformer { get; set; } = new CustomizedCryptoTransformer();
        }
#endif
        static void Main(string[] args)
        {
            var connectorServer = new CustomizedConnectorServer();
            connectorServer.OnError += ConnectorServer_OnError;
            connectorServer.OnDataReceived += connectorClientDataReceived;
            connectorServer.Start();
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                abort = true; 
            };
            ConsoleIOBinder.OnError += ConsoleIOBinder_OnError;
            ConsoleIOBinder.Bind(connectorServer);
            new Thread(runDumpConsoleWrites) { IsBackground = true }.Start();
            SpinWait.SpinUntil(() => abort);
            ConsoleIOBinder.UnBind();
            connectorServer.Stop();
            Console.WriteLine("stopped");
            Console.ReadKey(true);
        }

        private static void ConsoleIOBinder_OnError(Exception obj) => Console.WriteLine($"[{nameof(ConsoleIOBinder_OnError)}] {obj}");

        private static void ConnectorServer_OnError(Exception obj) => Console.WriteLine($"[{nameof(ConnectorServer_OnError)}] {obj}");

        private static void runDumpConsoleWrites()
        {
            while (true)
            {
                Console.WriteLine("Server console output");
                Debug.WriteLine("Server debug output");
                Debug.Print("Server print debug output");
                Trace.WriteLine("Server trace output");
                Trace.TraceInformation("Server trace information output");
                Trace.TraceWarning("Server trace warning output");
                Trace.TraceError("Server trace error output");
                Thread.Sleep(1000);
            }
        }

        private static void connectorClientDataReceived(ConnectorBase connector, Stream stream, byte[] obj) {
            Console.WriteLine($"rec: ({Encoding.ASCII.GetString(obj)})");
            connector.Send(stream, obj);
        }
    }
}
