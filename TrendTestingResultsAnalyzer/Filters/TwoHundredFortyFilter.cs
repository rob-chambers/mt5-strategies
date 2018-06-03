using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class TwoHundredFortyFilter : Filter
    {
        public override string Name => "Above/Below 240";

        public override string Description => "Only take trades when the price is above the 240 LMA (enter argument to switch)";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (string.IsNullOrWhiteSpace(ArgumentValue))
            {
                return trade.Direction == TradeDirection.Long
                    ? trade.EntryPrice > trade.MA240
                    : trade.EntryPrice < trade.MA240;
            }

            // Reverse
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice < trade.MA240
                : trade.EntryPrice > trade.MA240;
        }
    }
}
