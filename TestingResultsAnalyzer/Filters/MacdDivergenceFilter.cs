using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class MacdDivergenceCrossFilter : Filter
    {
        public override string Name => "MACD Divergence with cross";

        public override string Description => "For long trades, only enter when the MACD is moving up and has crossed the zero line.";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.UpCrossRecentIndex > -1 && trade.UpCrossPriorIndex > -1 && trade.UpCrossRecentValue > trade.UpCrossPriorValue &&
                    trade.UpCrossRecentValue > 0 && trade.UpCrossPriorValue < 0
                : trade.DownCrossRecentIndex > -1 && trade.DownCrossPriorIndex > -1 && trade.DownCrossRecentValue < trade.DownCrossPriorValue &&
                    trade.DownCrossRecentValue < 0 && trade.DownCrossPriorValue > 0;
        }
    }
}
