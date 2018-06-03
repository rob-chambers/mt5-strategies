using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class FarAwayFilter : Filter
    {
        public override string Name => "Far Away";

        public override string Description => "Testing counter-trend trades - only go short when the price is above all 3 MAs";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice < trade.MA240 && trade.EntryPrice < trade.MA50
                : trade.EntryPrice > trade.MA240 && trade.EntryPrice > trade.MA50;
        }
    }
}
