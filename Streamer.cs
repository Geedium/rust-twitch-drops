using System.Diagnostics;

namespace WindowsFormsApp2
{
    /// <summary>
    /// Streamer
    /// </summary>
    public class Streamer
    {
        public string identifier;
        public string twitch;
        private bool status;
        public Stopwatch elapsed;

        public string Status
        {
            set
            {
                status = value == "Live" ? true : false;
            }
        }

        public bool IsStreaming
        {
            get
            {
                return status;
            }
        }
    }
}