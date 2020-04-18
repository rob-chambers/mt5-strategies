namespace Powder.TradingLibrary
{
    public class Alert
    {
        public string Indicator { get; private set; }
        public string Pair { get; private set; }
        public string TimeFrame { get; private set; }

        public Alert(string indicator, string pair, string timeFrame)
        {
            Indicator = indicator;
            Pair = pair;
            TimeFrame = timeFrame;
        }
    }
}