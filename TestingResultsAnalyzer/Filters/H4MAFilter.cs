using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class H4MAFilter : Filter
    {
        public override string Name => "H4 MA";

        public override string Description => "Only take trades when the price is higher than the 4 hourly moving average (or lower than the MA when going short)";

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.EntryPrice > trade.H4MA
                : trade.EntryPrice < trade.H4MA;
        }
    }
}
