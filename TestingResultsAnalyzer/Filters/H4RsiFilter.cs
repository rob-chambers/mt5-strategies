using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class H4RsiFilter : Filter
    {
        public override string Name => "H4 RSI";

        public override string Description => "For long trades, only enter when 4 hourly RSI < 70.  For short trades, only enter when 4 hourly RSI > 30.";

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.H4Rsi < 70
                : trade.H4Rsi > 30;
        }
    }
}
