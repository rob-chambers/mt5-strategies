using System;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class FlatTrendFilter : Filter
    {
        public override string Name => "Flat Trend";

        public override string Description => "Avoid taking trades when the trend is flat";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                        ? trade.UpCrossRecentIndex > -1 && trade.UpCrossPriorIndex > -1 && Math.Abs(trade.UpCrossRecentPrice - trade.UpCrossPriorPrice) >= 0.0005
                        : trade.DownCrossRecentIndex > -1 && trade.DownCrossPriorIndex > -1 && Math.Abs(trade.DownCrossRecentPrice - trade.DownCrossPriorPrice) >= 0.0005;
        }
    }
}
