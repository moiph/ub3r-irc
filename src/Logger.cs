namespace UB3RIRC
{
    using System;
    using System.Collections.Generic;

    public class LogData
    {
        public LogType LogType { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public LogData(LogType logType, string message, Exception exception = null)
        {
            this.LogType = logType;
            this.Message = message;
            this.Exception = exception;
        }
    }

    /// <summary>
    /// A logger
    /// </summary>
    public class Logger
    {
        private List<ILog> loggers;
        private LogType verbosity;

        /// <summary>
        /// Helper to get a quick console logger.
        /// </summary>
        /// <returns>Logger instance.</returns>
        public static Logger GetConsoleLogger()
        {
            return new Logger(LogType.Debug, new List<ILog> { new ConsoleLog() });
        }

        /// <summary>
        /// Log events
        /// </summary>
        public event LogEventHandler OnLogEvent;

        /// <summary>
        /// Log event handler.
        /// </summary>
        /// <param name="message">The message received from the server.</param>
        public delegate void LogEventHandler(LogData data);

        /// <summary>
        /// Initializes a new instance of Logger 
        /// </summary>
        /// <param name="verbosity">Level of verbosity for logging.</param>
        /// <param name="loggers"></param>
        public Logger(LogType verbosity, List<ILog> loggers)
        {
            this.verbosity = verbosity;
            this.loggers = new List<ILog>(loggers);
        }

        /// <summary>
        /// Adds the given log medium to the internal list.
        /// </summary>
        /// <param name="logMedium">The log medium to add.</param>
        public void AddLogger(ILog logMedium)
        {
            this.loggers.Add(logMedium);
        }

        /// <summary>
        /// Logs the given text.
        /// </summary>
        /// <param name="logType">The type of log (debug, error, etc)</param>
        /// <param name="text">The text to log.</param>
        public void Log(LogType logType, string text)
        {
            this.Log(logType, text, null);
        }

        /// <summary>
        /// Logs the given text; supports as a string format.
        /// </summary>
        /// <param name="logType">The type of log (debug, error, etc)</param>
        /// <param name="text">The text to log.</param>
        /// <param name="args">The string format arguments.</param>
        public void Log(LogType logType, string text, Exception exception = null)
        {
            this.OnLogEvent?.Invoke(new LogData(logType, text, exception));

            switch (logType)
            {
                case LogType.Debug:
                    if (this.verbosity <= LogType.Debug)
                    {
                        this.loggers.ForEach(l => l.Debug(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Info:
                    if (this.verbosity <= LogType.Info)
                    {
                        this.loggers.ForEach(l => l.Info(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Warn:
                    if (this.verbosity <= LogType.Warn)
                    {
                        this.loggers.ForEach(l => l.Warn(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Error:
                    if (this.verbosity <= LogType.Error)
                    {
                        this.loggers.ForEach(l => l.Error(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Fatal:
                    if (this.verbosity <= LogType.Fatal)
                    {
                        this.loggers.ForEach(l => l.Fatal(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Incoming:
                    if (this.verbosity <= LogType.Incoming)
                    {
                        this.loggers.ForEach(l => l.Incoming(text, DateTime.Now.Ticks));
                    }
                    break;

                case LogType.Outgoing:
                    if (this.verbosity <= LogType.Outgoing)
                    {
                        this.loggers.ForEach(l => l.Outgoing(text, DateTime.Now.Ticks));
                    }
                    break;

                default:
                    throw new ArgumentException($"Unrecognized LogType specified: {logType}");
            }
        }
    }
}
