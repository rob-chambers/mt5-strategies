using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class TwoHundredFortyFilter : Filter
    {
        public override string Name => "240";

        public override string Description => "Only take trades when the price is above the 240 LMA";

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice > trade.MA240
                : trade.EntryPrice < trade.MA240;
        }
    }
}
