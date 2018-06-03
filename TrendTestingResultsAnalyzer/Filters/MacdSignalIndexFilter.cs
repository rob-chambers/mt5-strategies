using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class MacdSignalIndexFilter : Filter
    {
        public override string Name => "MACD Signal Index";

        public override string Description => "Only enter when there is a MACD signal within the last x number of bars";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!int.TryParse(ArgumentValue, out int value))
            {
                return false;
            }

            return trade.Direction == TradeDirection.Long
                ? (trade.UpIdx != -1 && trade.UpIdx < value)
                : (trade.DownIdx != -1 && trade.DownIdx < value);
        }
    }
}
