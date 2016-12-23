namespace UB3RIRC
{
    using System;

    class ConsoleLog : ILog
    {
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

        private object consoleLock = new object();
        private void WriteToConsole(string prefix, string text, long ticks, ConsoleColor? color = null)
        {
            lock (consoleLock)
            {
                var currentColor = Console.ForegroundColor;
                color = color.HasValue ? color.Value : currentColor;

                Console.Write(new DateTime(ticks).ToString("HH:mm:ss"));
                Console.ForegroundColor = color.Value;
                Console.Write(" " + prefix + " ");
                Console.ForegroundColor = currentColor;
                Console.WriteLine(text);
            }
        }
    }
}
