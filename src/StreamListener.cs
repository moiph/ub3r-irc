
namespace UB3RIRC
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Listens for incoming data from the network stream.
    /// </summary>
    public class StreamListener : IDisposable
    {
        private readonly Connection connection;
        private StreamReader streamReader;
        private Task listenerTask;

        private bool isDisposed;
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Initializes an instance of StreamListener which asynchrounsly handles reads from the network stream.
        /// </summary>
        internal StreamListener(Connection connection, Stream stream)
        {
            tokenSource = new CancellationTokenSource();

            this.connection = connection;
            this.streamReader = new StreamReader(stream);

            var token = tokenSource.Token;

            this.listenerTask = Task.Factory.StartNew((object state) =>
            {
                this.Read(token);
            }, token);
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
                    this.streamReader.Dispose();
                }

                this.isDisposed = true;
            }
        }

        /// <summary>
        /// The logger to use.
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// Reads from the stream.
        /// </summary>
        public void Read(CancellationToken token)
        {
            string line = string.Empty;
            try
            {
                while ((line = this.streamReader.ReadLine()) != null)
                {
                    this.Logger.Log(LogType.Incoming, line);
                    connection.IncomingMessage(line);

                    // TODO:
                    // Remove this? Kinda fun.
                    // Test hook to fake disconnects and attempt reconnection.
                    if (line.Contains("DIE, UB3R-B0T"))
                    {
                        break;
                    }
                }

                this.Logger.Log(LogType.Warn, "Empty line read");
            }
            catch (IOException e)
            {
                this.Logger.Log(LogType.Error, "Caught IOexception when reading from stream: {0}", e);
            }
            finally
            {
                this.Logger.Log(LogType.Warn, "Read loop exited, likely disconnected. Attempting to reconnect.");
                connection.Disconnect(shouldFireEvent: true);
                this.tokenSource.Cancel();
            }
        }
    }
}
