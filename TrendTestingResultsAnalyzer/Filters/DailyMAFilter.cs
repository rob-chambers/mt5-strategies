using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class DailyMAFilter : Filter
    {
        public override string Name => "Daily MA";

        public override string Description => "Only take trades when the price is higher than the Daily timeframe moving average (or lower than the MA when going short)";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice > trade.DailyMA1
                : trade.EntryPrice < trade.DailyMA1;
        }
    }
}
