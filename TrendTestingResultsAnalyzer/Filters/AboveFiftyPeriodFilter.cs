using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class AboveFiftyPeriodFilter : Filter
    {
        public override string Name => "Above/Below 50";

        public override string Description => "Only take long trades when the price is above the 50 period EMA (enter argument to switch)";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (string.IsNullOrWhiteSpace(ArgumentValue))
            {
                return trade.Direction == TradeDirection.Long
                    ? trade.EntryPrice > trade.MA50
                    : trade.EntryPrice < trade.MA50;
            }

            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice < trade.MA50
                : trade.EntryPrice > trade.MA50;
        }
    }
}
