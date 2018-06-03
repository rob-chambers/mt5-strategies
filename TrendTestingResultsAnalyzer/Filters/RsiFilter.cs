using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class RsiFilter : Filter
    {
        public RsiFilter()
        {
            ArgumentValue = "30";
        }

        public override string Name => "RSI";

        public override string Description => "For long trades, only enter when the RSI <= 30.  For short trades, only enter when the RSI >= 70";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!int.TryParse(ArgumentValue, out int value))
            {
                return false;
            }

            return trade.Direction == TradeDirection.Long
                ? trade.RsiCurrent <= value
                : trade.RsiCurrent >= (100 - value);
        }
    }
}
