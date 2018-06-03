using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class LateFridayFilter : Filter
    {
        public override string Name => "Late Friday";

        public override string Description => "Only take long trades when it's not Friday afternoon.  Argument represents hour of day";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            var day = trade.EntryDateTime;

            if (!int.TryParse(ArgumentValue, out int hour))
            {
                return false;
            }

            if (day.DayOfWeek == System.DayOfWeek.Friday && day.TimeOfDay.Hours > hour)
            {
                return false;
            }

            return true;
        }
    }
}
