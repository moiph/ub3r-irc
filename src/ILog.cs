namespace UB3RIRC
{
    /// <summary>
    /// The types of logging available.
    /// </summary>
    public enum LogType
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
        Incoming,
        Outgoing
    }

    /// <summary>
    /// Basic interface for log mediums.
    /// </summary>
    public interface ILog
    {
        void Debug(string text, long ticks);

        void Info(string text, long ticks);

        void Warn(string text, long ticks);

        void Error(string text, long ticks);

        void Fatal(string text, long ticks);

        void Incoming(string text, long ticks);

        void Outgoing(string text, long ticks);
    }
}
