
namespace UB3RIRC
{
    using System.Text.RegularExpressions;

    public class MessageData
    {
        private static Regex NickRx = new Regex(@":([^!:]+)!([^:\s]+)", RegexOptions.Compiled);
        private string[] textParts;

        /// <summary>
        /// The verb / action / command (e.g. PRIVMSG)
        /// </summary>
        public string Verb { get; private set; }

        /// <summary>
        /// The source of the message
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// The target of the message
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// The text content of the message.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// A tokenized copy of the message text, split on spaces.
        /// </summary>
        public string[] TextParts
        {
            get
            {
                if (this.textParts == null && this.Text != null)
                {
                    this.textParts = this.Text.Split(new[] { ' ' });
                }

                return this.textParts;
            }
        }

        /// <summary>
        /// The nickname parsed out from the source.
        /// </summary>
        public string Nick { get; private set; }

        /// <summary>
        /// The host parsed out from the source.
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Parses a message into a MessageData object.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A MessageData instance.</returns>
        public static MessageData Parse(string message)
        {
            var data = new MessageData();

            string[] parts = message.Split(new[] { ' ' }, 4);

            switch (parts.Length)
            {
                case 2:
                    data.Verb = parts[0];
                    data.Text = parts[1];

                    break;

                case 3:
                case 4:
                    data.Source = parts[0];
                    data.Verb = parts[1];
                    data.Target = parts[2];
                    data.Text = string.Empty;

                    Match match = NickRx.Match(message);
                    if (match.Success && match.Groups.Count == 3)
                    {
                        data.Nick = match.Groups[1].Value;
                        data.Host = match.Groups[2].Value;
                    }

                    if (parts.Length == 4)
                    {
                        data.Text = parts[3].StartsWith(":") ? parts[3].Substring(1) : parts[3];
                    }

                    break;
            }

            return data;
        }
    }
}
