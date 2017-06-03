
namespace UB3RIRC
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Maintains a connection with the server and handles reads/writes.
    /// </summary>
    public class Connection : IDisposable
    {
        private bool isDisposed = false;

        private TcpClient tcpClient;
        private StreamListener listener;

        private StreamWriter streamWriter;

        private Timer pingTimer;

        /// <summary>
        /// The server to connect to.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// The port to connect to.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Whether or not to use SSL.
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Whether or not to validate the server's certificate; applies when connecting to servers with SSL.
        /// Defaults to true.
        /// </summary>
        public bool ShouldValidateServerCertificate { get; set; }

        /// <summary>
        /// An optional client certificate to use.
        /// </summary>
        public X509Certificate ClientCertificate { get; set; }

        /// <summary>
        /// The logger to use.
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// Whether or not we're connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Incoming message events
        /// </summary>
        public event IncomingMessageHandler OnIncomingMessage;

        /// <summary>
        /// Incoming message event handler.
        /// </summary>
        /// <param name="message">The message received from the server.</param>
        public delegate void IncomingMessageHandler(string message);

        /// <summary>
        /// Disconnect events
        /// </summary>
        public event DisconnectHandler OnDisconnect;

        /// <summary>
        /// Disconnect event handler
        /// </summary>
        public delegate void DisconnectHandler();

        /// <summary>
        /// Initializes an instance of Connection which is the networking component to the server.
        /// </summary>
        /// <param name="server">The server to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="useSsl">Whether or not to use SSL.</param>
        public Connection(string server, int port, bool useSsl)
        {
            this.Server = server;
            this.Port = port;
            this.UseSsl = useSsl;
            this.ShouldValidateServerCertificate = true;
        }

        /// <summary>
        /// Creates a connection to the server.
        /// </summary>
        public async Task ConnectAsync()
        {
            // Setup the connection
            this.Logger.Log(LogType.Info, "Connecting to {0}", this.Server);

            try
            {
                this.tcpClient = new TcpClient();
                await this.tcpClient.ConnectAsync(this.Server, this.Port);

                // Reader and writer
                Stream stream = this.tcpClient.GetStream();

                //
                // TODO: Enable this once we can move to net stanard 2.0 (which should bring SslStream support)
                //
                /*if (this.UseSsl)
                {
                    var remoteCertificateValidationCallback = new RemoteCertificateValidationCallback(
                        (object sender,
                            X509Certificate certificate,
                            X509Chain chain,
                            SslPolicyErrors sslPolicyErrors) =>
                        {
                            if (!this.ShouldValidateServerCertificate || sslPolicyErrors == SslPolicyErrors.None)
                            {
                                return true;
                            }

                            this.Logger.Log(LogType.Warn, "Certificate error: {0}", sslPolicyErrors);

                            // Do not allow this client to communicate with unauthenticated servers. 
                            return false;
                        });

                    // MSDN reference: http://msdn.microsoft.com/en-us/library/system.net.security.localcertificateselectioncallback(v=vs.110).aspx
                    var localCertificateValidationCallback = new LocalCertificateSelectionCallback(
                        (object sender,
                            string targetHost,
                            X509CertificateCollection localCertificates,
                            X509Certificate remoteCertificate,
                            string[] acceptableIssuers) =>
                        {
                            if (acceptableIssuers != null && acceptableIssuers.Length > 0 && localCertificates != null &&
                                localCertificates.Count > 0)
                            {
                                // Use the first certificate that is from an acceptable issuer. 
                                foreach (var certificate in from X509Certificate certificate in localCertificates
                                                            let issuer = certificate.Issuer
                                                            where Array.IndexOf(acceptableIssuers, issuer) != -1
                                                            select certificate)
                                {
                                    return certificate;
                                }
                            }

                            if (localCertificates != null && localCertificates.Count > 0)
                            {
                                return localCertificates[0];
                            }

                            return null;
                        });

                    var sslStream = new SslStream(
                        stream,
                        false,
                        remoteCertificateValidationCallback,
                        localCertificateValidationCallback);

                    try
                    {
                        if (this.ClientCertificate != null)
                        {
                            var certCollection = new X509CertificateCollection { this.ClientCertificate };
                            sslStream.AuthenticateAsClient(
                                this.Server,
                                certCollection,
                                SslProtocols.Default,
                                checkCertificateRevocation: true);
                        }
                        else
                        {
                            sslStream.AuthenticateAsClient(this.Server);
                        }

                        stream = sslStream;
                    }
                    catch (AuthenticationException e)
                    {
                        this.Logger.Log(LogType.Warn, "Failed to authenticate SslStream: {0}", e.Message);

                        if (e.InnerException != null)
                        {
                            this.Logger.Log(LogType.Error, "Inner exception: {0}", e.InnerException.Message);
                        }

                        this.tcpClient.Close();

                        // TODO:
                        // New exception type
                        throw new NotConnectedException("Failed to Connect");
                    }
                } */

                this.streamWriter = new StreamWriter(stream);

                // Listen for messages from the server
                this.listener = new StreamListener(this, stream) { Logger = this.Logger };

                this.Logger.Log(LogType.Debug, "Listening for messages.");

                this.IsConnected = true;

                this.pingTimer?.Dispose();
                this.pingTimer = new Timer(this.Ping, null, 120000, 120000);
            }
            catch (SocketException e)
            {
                // Failed to connect.

                // TODO:
                // Add client configuration to attempt retries
                this.Logger.Log(LogType.Error, $"Caught IOexception when reading from stream: {e}");
            }
            catch (NotConnectedException)
            {
                // TODO:
                // Improve this error handling
            }
        }

        public void Ping(object state)
        {
            this.Write($"PING {Utime}");
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect(bool shouldFireEvent = false)
        {
            this.Logger.Log(LogType.Info, "Disconnecting from {0}", this.Server);
            this.CloseConnections();

            if (shouldFireEvent && this.OnDisconnect != null)
            {
                this.OnDisconnect();
            }
        }

        /// <summary>
        /// Writes the given text to the stream.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public void Write(string text)
        {
            this.Write(text, null);
        }

        /// <summary>
        /// Writes the given text to the stream, using the passed in a string format.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="args">Format arguments.</param>
        public void Write(string text, params object[] args)
        {
            if (!this.IsConnected)
            {
                throw new NotConnectedException(string.Format("Server {0} is not connected.", this.Server));
            }

            if (args != null)
            {
                try
                {
                    text = string.Format(text, args);
                }
                catch (FormatException)
                {
                    throw new ArgumentException(string.Format("Invalid arguments list, expected pattern: {0}", text));
                }
            }

            this.Logger.Log(LogType.Outgoing, text);

            this.streamWriter.WriteLine(text);
            this.streamWriter.Flush();
        }

        /// <summary>
        /// Fires the OnIncomingMessage event with the line read from the stream.
        /// </summary>
        /// <param name="line">The current line read from the stream.</param>
        public void IncomingMessage(string line)
        {
            this.OnIncomingMessage?.Invoke(line);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper to get a unix timestamp.
        /// </summary>
        public static long Utime
        {
            get
            {
                TimeSpan span = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                return (int)span.TotalSeconds;
            }
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.Logger.Log(LogType.Info, "Disposing");

                if (disposing)
                {
                    this.CloseConnections();
                }

                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Closes up connections.
        /// </summary>
        private void CloseConnections()
        {
            this.streamWriter.Dispose();

            this.tcpClient.Dispose();
            this.listener.Dispose();

            this.IsConnected = false;
        }
    }

    /// <summary>
    /// Thrown when trying to write when not connected.
    /// </summary>
    public class NotConnectedException : Exception
    {
        public NotConnectedException(string message)
            : base(message)
        {
        }
    }
}
