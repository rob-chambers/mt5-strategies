using System;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class ChangingTrendFilter : Filter
    {
        public ChangingTrendFilter()
        {
            ArgumentValue = "2";
        }

        public override string Name => "Changing Trend";

        public override string Description => "Avoid taking trades when we get conflicting signals within a couple of bars of one another";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!int.TryParse(ArgumentValue, out int value))
            {
                return false;
            }

            return trade.Direction == TradeDirection.Long
                    ? trade.DownCrossRecentIndex < trade.UpCrossRecentIndex || (trade.DownCrossRecentIndex - trade.UpCrossRecentIndex <= value)
                    : trade.DownCrossRecentIndex > trade.UpCrossRecentIndex || (trade.UpCrossRecentIndex - trade.DownCrossRecentIndex <= value);
        }
    }
}
