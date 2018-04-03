using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class PriceToMacdDivergenceFilter : Filter
    {
        public override string Name => "Price to MACD Divergence";

        public override string Description => "For long trades, only enter when the MACD is moving up but price has moved down.";

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.UpCrossRecentIndex > -1 && trade.UpCrossPriorIndex > -1 && trade.UpCrossRecentValue > trade.UpCrossPriorValue &&
                    trade.UpCrossRecentPrice < trade.UpCrossPriorPrice
                : trade.DownCrossRecentIndex > -1 && trade.DownCrossPriorIndex > -1 && trade.DownCrossRecentValue < trade.DownCrossPriorValue &&
                    trade.DownCrossRecentPrice > trade.DownCrossPriorPrice;
        }
    }
}
