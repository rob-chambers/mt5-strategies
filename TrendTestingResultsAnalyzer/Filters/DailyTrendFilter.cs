using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class DailyTrendFilter : Filter
    {
        public override string Name => "Daily Trend";

        public override string Description => "Only go long when the daily trend is flat or up (set argument to ignore flat trends)";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            bool up = false;
            bool includeFlat = true;

            if (!string.IsNullOrWhiteSpace(ArgumentValue))
            {
                includeFlat = false;
            }

            switch (trade.DailyTrend)
            {
                case TrendType.Flat:
                    return includeFlat;

                case TrendType.Up:
                case TrendType.SoftUp:
                case TrendType.HardUp:
                    up = true;
                    break;
            }

            return trade.Direction == TradeDirection.Long
                ? up
                : !up;
        }
    }
}
