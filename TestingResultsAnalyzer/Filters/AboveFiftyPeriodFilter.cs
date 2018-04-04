using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class AboveFiftyPeriodFilter : Filter
    {
        public override string Name => "Above 50";

        public override string Description => "Only take long trades when the price is above the 50 period EMA";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice > trade.MA50
                : trade.EntryPrice < trade.MA50;
        }
    }
}
