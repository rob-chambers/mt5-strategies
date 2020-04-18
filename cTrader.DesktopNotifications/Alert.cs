using System;

namespace cTrader.DesktopNotifications
{
    public class Alert : EventArgs
    {
        public DateTime TriggerTimeStamp { get; set; }
        public string Indicator { get; set; }
        public string Pair { get; set; }
        public string TimeFrame { get; set; }
    }
}
