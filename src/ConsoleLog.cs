namespace UB3RIRC
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class ConsoleLog : ILog
    {
        BlockingCollection<LogMessage> logMessageCollection = new BlockingCollection<LogMessage>();
        private static object consoleLock = new object();

        public ConsoleLog()
        {
            Task.Run(() =>
            {
                foreach (var message in logMessageCollection.GetConsumingEnumerable())
                {
                    lock (consoleLock)
                    {
                        var currentColor = Console.ForegroundColor;
                        ConsoleColor? color = message.Color ?? currentColor;

                        Console.Write(new DateTime(message.Ticks).ToString("HH:mm:ss"));
                        Console.ForegroundColor = color.Value;
                        Console.Write(" " + message.Prefix + " ");
                        Console.ForegroundColor = currentColor;
                        Console.WriteLine(message.Text);
                    }
                }
            });
        }

        ~ConsoleLog()
        {
            logMessageCollection.CompleteAdding();
        }

        public void Debug(string text, long ticks)
        {
            this.WriteToConsole("+++", text, ticks);
        }

        public void Info(string text, long ticks)
        {
            this.WriteToConsole("+++", text, ticks);
        }

        public void Warn(string text, long ticks)
        {
            this.WriteToConsole("+++", text, ticks, ConsoleColor.Yellow);
        }

        public void Error(string text, long ticks)
        {
            this.WriteToConsole("!!!", text, ticks, ConsoleColor.Red);
        }

        public void Fatal(string text, long ticks)
        {
            this.WriteToConsole("!!!", text, ticks, ConsoleColor.DarkRed);
        }

        public void Incoming(string text, long ticks)
        {
            this.WriteToConsole("<<<", text, ticks, ConsoleColor.Green);
        }

        public void Outgoing(string text, long ticks)
        {
            this.WriteToConsole(">>>", text, ticks, ConsoleColor.Blue);
        }

        
        private void WriteToConsole(string prefix, string text, long ticks, ConsoleColor? color = null)
        {
            logMessageCollection.Add(new LogMessage
            {
                Prefix = prefix,
                Text = text,
                Ticks = ticks,
                Color = color,
            });
        }
    }

    internal class LogMessage
    {
        public string Prefix { get; set; }
        public string Text { get; set; }
        public long Ticks { get; set; }
        public ConsoleColor? Color { get; set; }
    }
}
