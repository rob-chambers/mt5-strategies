using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class MACDValueFilter : Filter
    {
        public override string Name => "MACD Value";

        public override string Description => "Only take trades when the MACD is above/below a certain value";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!double.TryParse(ArgumentValue, out double value))
            {
                return false;
            }

            return trade.Direction == TradeDirection.Long
                ? trade.MACD < -value
                : trade.MACD > value;
        }
    }
}
