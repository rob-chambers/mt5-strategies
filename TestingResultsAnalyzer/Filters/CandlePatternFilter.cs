using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class CandlePatternFilter : Filter
    {
        public override string Name => "Candle Pattern";

        public override string Description => "Only go long when we have a close near the high";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            var distanceFromLow = trade.EntryPrice - trade.Low;
            var distanceFromHigh =  trade.High - trade.EntryPrice;

            var bullish = distanceFromLow > 0 && distanceFromHigh / distanceFromLow <= 0.33;
            var bearish = distanceFromHigh > 0 && distanceFromLow / distanceFromHigh <= 0.33;

            return trade.Direction == TradeDirection.Long
                ? bullish
                : bearish;
        }
    }
}
