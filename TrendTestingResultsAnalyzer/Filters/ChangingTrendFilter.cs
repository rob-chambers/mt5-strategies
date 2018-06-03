using System;
using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
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
                    ? trade.DownIdx < trade.UpIdx || (trade.DownIdx - trade.UpIdx <= value)
                    : trade.DownIdx > trade.UpIdx || (trade.UpIdx - trade.DownIdx <= value);
        }
    }
}
