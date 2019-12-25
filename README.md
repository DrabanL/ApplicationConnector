# ApplicationConnector
This library was designed with the intention to provide a simple way to communicate between applications (Server<->Client).

For now, Pipeline and Sockets are supported as the underlying communication providers.


Console, Debug and Trace binders are already implemented and can be used to get the application output and broadcast it.

A use case for this library may be, for example, if you want to connect a Windows Service, on demand (while the service is running) to an external application, and process IO to\from the service. (you can use the provided DataTransforms to encrypt the IO data)

Another use case may be to live debug a Windows Service, when needed.

## Server-Side Example (Send output and receive client commands)
``
using RabanSoft.ApplicationConnector.ConnectorHandlers;

..

var connectorServer = new PipelineConnectorServer(); // new SocketConnectorServer(customPort);
connectorServer.OnError += ..;
connectorServer.OnDataReceived += ..;
connectorServer.Start();

..

connectorServer.Send(..);

..

connectorServer.Stop();
``

## Client-Side Example (Send commands and receive server output)
``
using RabanSoft.ApplicationConnector.ConnectorHandlers;

..

var connectorClient = new PipelineConnectorClient("serverProcessName"); // new SocketConnectorClient(customPort);
connectorClient.OnError += ..;
connectorClient.OnDataReceived += ..;
connectorClient.Start();

..

connectorClient.Send(..);

..

connectorClient.Stop();
``

## Data Encryption Example
* It is recommanded to use DataTransformers to encrypt the outgoing and incoming data so it will not be visible to MITM attacks, as well as to let only a verified consumer process to communicate with the producer process
``
using RabanSoft.ApplicationConnector.DataTransformers;

..

class CustomizedCryptoTransformer : TripleDESCryptoDataTransformer
{
  public override string SecretKey { get; set; } = "YourSecretPassword";
}

..

class CustomizedConnectorServer /** CustomizedConnectorClient */ : PipelineConnectorServer // PipelineConnectorClient/SocketConnectorServer/SocketConnectorClient
{
  public override IDataTransformer DataTransformer { get; set; } = new CustomizedCryptoTransformer();
}

var connectorServerBaseInstance = new CustomizedConnectorServer();
``

## Console Output Binding Example (Producer process)
* This binder gets Console.Write calls and broadcasts them using the connector server instance
``
using RabanSoft.ApplicationConnector.IOBinders;

..

ConsoleIOBinder.OnError += ..;
ConsoleIOBinder.Bind(connectorServerBaseInstance);

..

ConsoleIOBinder.UnBind();
``

## Process Output Binding Example (Consumer process)
* This binder only gets Debug.Write or Trace.Write output from the producer
* The consumer actually attempts to attach a "debugger" to the producer process, which means the consumer process must have required previliges to attach to the other process, the two proccesses must be built in the same architecture (x86 or x64), and the producer process can be attached to only once.
* <code>ProcessIOBinder.DetachAll()</code> must be called by the consumer process before it is terminated, otherwise the producer process will terminate together with the consumer.
``
using RabanSoft.ApplicationConnector.IOBinders;

..

ProcessIOBinder.OnData += ..;
ProcessIOBinder.OnError += ..;
ProcessIOBinder.Attach("DestinationProcessName");

..

ProcessIOBinder.DetachAll();
``
