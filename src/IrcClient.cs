
namespace UB3RIRC
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class IrcClient
    {
        private Protocol protocol;
        private Logger logger;

        private const int connectionRetryDelay = 15000;

        /// <summary>
        /// An identifier for this client instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The server to connect to.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The nickname to use.
        /// </summary>
        public string Nick { get; set; }

        /// <summary>
        /// The password to connect to the server (optional)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Incoming message events
        /// </summary>
        public event IrcEventHandler OnIrcEvent;

        /// <summary>
        /// Incoming messege event handler.
        /// </summary>
        /// <param name="message">The message received from the server.</param>
        public delegate void IrcEventHandler(MessageData data, IrcClient client);

        /// <summary>
        /// Whether or not this client is connected.
        /// </summary>
        public bool IsConnected
        {
            get { return this.protocol.IsConnected; }
        }

        /// <summary>
        /// Event handle for a protocol event.
        /// </summary>
        /// <param name="data">The message data.</param>
        public void OnProtocolIrcEvent(MessageData data)
        {
            this.OnIrcEvent?.Invoke(data, this);
        }

        /// <summary>
        /// Whether or not we're hooked up and listening to events.
        /// </summary>
        public bool IsListeningToEvents { get; set; }

        /// <summary>
        /// The magical constructor.
        /// </summary>
        /// <param name="id">The identifier for this instance.</param>
        /// <param name="nick">The nickname to use.</param>
        /// <param name="host">The host to connec to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="useSsl">Whether or not to use SSL.</param>
        /// <param name="password">The optional password to use when connecting.</param>
        /// <param name="logVerbosity">The log verbosity.</param>
        public IrcClient(string id, string nick, string host, int port, bool useSsl, string password = null, LogType logVerbosity = LogType.Debug)
        {
            this.Id = id;
            this.Nick = nick;
            this.Host = host;
            this.Password = password;

            // setup loggers
            this.logger = new Logger(logVerbosity, new List<ILog> { new ConsoleLog() });

            this.protocol = new Protocol(host, port, useSsl, logger);
            this.protocol.OnIrcEvent += this.OnProtocolIrcEvent;
            this.protocol.Connection.OnDisconnect += Connection_OnDisconnect;
        }

        /// <summary>
        /// Sets a certificate to use on the connection to the server.
        /// Only used if configured to use SSL.
        /// </summary>
        /// <param name="path">Path to the certificate.</param>
        /// <param name="password">Optional password for the certificate.</param>/
        /// <remarks>
        /// Currently this only supports PKCS7 certificates. Using an RSA private key
        /// e.g. when created via OpenSSL is not yet supported.
        /// </remarks>
        public void SetCertificate(string path, string password)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            this.protocol.Connection.ClientCertificate = new X509Certificate(path, password);
        }

        /// <summary>
        /// Connects to the server.
        /// </summary>
        public async Task ConnectAsync()
        {
            await this.protocol.ConnectAsync();

            if (this.IsConnected)
            {
                if (this.Password != null)
                {
                    this.protocol.Command("PASS", this.Password);
                }

                this.protocol.Command("NICK", Nick);
                this.protocol.Command("USER", Nick, Nick);
            }
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect(string quitMessage = null)
        {
            this.protocol.Diconnect(quitMessage);
        }

        /// <summary>
        /// Sends a command to the server.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="args">Any arguments to pass through with the command.</param>
        public void Command(string command, params string[] args)
        {
            this.protocol.Command(command, args);
        }

        /// <summary>
        /// Event handler for disconnect events from the underlying connection.
        /// Only triggered if the disconnect was unexpected.
        /// This will kick off a timer to attempt reconnection with the server.
        /// </summary>
        private async void Connection_OnDisconnect()
        {
            // Wait before retrying
            await Task.Delay(connectionRetryDelay);

            await this.ConnectAsync();

            if (this.IsConnected)
            {
                this.logger.Log(LogType.Info, "Connection attempt to {0} succeeded.", this.Host);
            }
            else
            {
                this.logger.Log(LogType.Info, "Connection attempt to {0} failed. Retrying in {1} seconds...", this.Host, connectionRetryDelay);
            }
        }
    }
}
