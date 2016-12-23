
namespace UB3RIRC
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Supported commands.
    /// </summary>
    public enum CommandName
    {
        Unknown,
        Nick,
        User,
        Pass,
        Join,
        Part,
        Quit,
        Privmsg,
        Action,
        Topic,
        Kick,
        Mode,
        Invite,
        Motd,
        Whois,
        Notice,
    }

    /// <summary>
    /// Supported reply codes.
    /// </summary>
    public class ReplyCode
    {
        public const string RPL_WHOISUSER = "311";
        public const string RPL_NAMREPLY = "353";
        public const string RPL_MOTDSTART = "375";
        public const string RPL_MOTD = "372";
        public const string RPL_ENDOFMOTD = "376";
        public const string RPL_NOMOTD = "422";
    }

    /// <summary>
    /// Handles the IRC protocol.
    /// </summary>
    public class Protocol
    {
        private const string DefaultQuitMessage = "Shutting down...Bye!";

        private Connection connection;
        private Logger Logger;

        public static Dictionary<string, string> Commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "User", "USER {0} 0 * :{1}" },
            { "Nick", "NICK {0}" },
            { "Pass", "PASS {0}" },
            { "Join", "JOIN {0} {1}" },
            { "Part", "PART {0}" },
            { "Topic", "TOPIC {0} :{1}" },
            { "Motd", "MOTD" },
            { "Privmsg", "PRIVMSG {0} :{1}" },
            { "Action", "PRIVMSG {0} :\u0001ACTION {1}\u0001" },
            { "Notice", "NOTICE { 0} :{1}" },
            { "Whois", "WHOIS {0}" },
        };

        /// <summary>
        /// Incoming message events
        /// </summary>
        public event IrcEventHandler OnIrcEvent;

        /// <summary>
        /// Incoming messege event handler.
        /// </summary>
        /// <param name="message">The message received from the server.</param>
        public delegate void IrcEventHandler(MessageData data);

        /// <summary>
        /// Initializes an instance of The IRC Protocol class. Exposes IRC 
        /// protocol functionality.
        /// </summary>
        /// <param name="server">The server to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="useSsl">Whether or not to use SSL.</param>
        /// <param name="logger">A Logger instance.</param>
        public Protocol(string server, int port, bool useSsl, Logger logger)
        {
            this.Logger = logger;

            this.connection = new Connection(server, port, useSsl)
            {
                Logger = logger,
            };

            this.connection.OnIncomingMessage += this.OnIncomingMessage;
        }

        /// <summary>
        /// The underlying connection.
        /// </summary>
        public Connection Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// Connects to the server.
        /// </summary>
        public async Task ConnectAsync()
        {
            await this.connection.ConnectAsync();
        }

        /// <summary>
        /// Disconnects from the sever.
        /// </summary>
        /// <param name="quitMessage">The quit message; if null, uses a default.</param>
        public void Diconnect(string quitMessage = null)
        {
            this.connection.Write("QUIT :{0}", quitMessage ?? DefaultQuitMessage);
            this.connection.Disconnect();
        }

        /// <summary>
        /// Whether or not we're connected to the server.
        /// </summary>
        public bool IsConnected
        {
            get { return this.connection.IsConnected; }
        }

        /// <summary>
        /// Callback for incoming message events.
        /// </summary>
        /// <param name="text">The text from the message.</param>
        public void OnIncomingMessage(string text)
        {
            MessageData data = MessageData.Parse(text);

            switch (data.Verb)
            {
                case "PING":
                    this.connection.Write("PONG {0}", data.Text);
                    break;

                case ReplyCode.RPL_ENDOFMOTD:
                case ReplyCode.RPL_NOMOTD:
                case "PRIVMSG":
                    this.OnIrcEvent?.Invoke(data);
                    break;

                default:
                    break;
                    //throw new InvalidOperationException("Unrecognized message sequence.");
            }
        }

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="args">Arguments for the command.</param>
        public void Command(string commandName, params string[] args)
        {
            string commandSyntax;
            if (!Protocol.Commands.TryGetValue(commandName.ToString(), out commandSyntax))
            {
                throw new ArgumentException(string.Format("Unrecognized command: {0}", commandName));
            }

            this.connection.Write(commandSyntax, args);
        }

        /// <summary>
        /// Sets the topic for the specified channel.
        /// </summary>
        /// <param name="target">The channel.</param>
        /// <param name="topic">The topic to set.</param>
        public void SetTopic(string target, string topic)
        {
            if (!target.StartsWith("#"))
            {
                throw new ArgumentException(string.Format("Target must be a channel; {0} does not start with #.", target));
            }

            this.connection.Write("TOPIC {0} {1}", target, topic);
        }
    }
}
