using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class HighestHigh20Filter : Filter
    {
        public override string Name => "Highest High 20";

        public override string Description => "Only take long trades when the close + range is above the 20 period highest high";

        public override bool IsCombinable => true;

        public override bool HasArgument => false;

        public override bool IsIncluded(TradeViewModel trade)
        {
            //if (string.IsNullOrWhiteSpace(ArgumentValue))
            //{
            //    return trade.Direction == TradeDirection.Long
            //        ? trade.EntryPrice > trade.MA50
            //        : trade.EntryPrice < trade.MA50;
            //}

            // TODO: Handle short trades
            return trade.Direction == TradeDirection.Long
                ? trade.Close + (trade.High - trade.Low) > trade.High20
                : false;
        }
    }
}
