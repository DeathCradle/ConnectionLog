using System;

namespace ConnectionLog.Models
{
    public class LogItem
    {
        public long Id { get; set; }
        public string PlayerName { get; set; }
        public string IpAddress { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
