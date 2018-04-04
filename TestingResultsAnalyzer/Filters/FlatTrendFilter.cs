using System;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class FlatTrendFilter : Filter
    {
        public FlatTrendFilter()
        {
            ArgumentValue = "0.005";
        }

        public override string Name => "Flat Trend";

        public override string Description => "Avoid taking trades when the trend is flat.  Looks at the slope of the line between the two most recent MACD signals";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!double.TryParse(ArgumentValue, out double value))
            {
                return false;
            }

            return trade.Direction == TradeDirection.Long
                    ? trade.UpCrossRecentIndex > -1 && trade.UpCrossPriorIndex > -1 && Math.Abs(trade.UpCrossRecentPrice - trade.UpCrossPriorPrice) >= value
                    : trade.DownCrossRecentIndex > -1 && trade.DownCrossPriorIndex > -1 && Math.Abs(trade.DownCrossRecentPrice - trade.DownCrossPriorPrice) >= value;
        }
    }
}
