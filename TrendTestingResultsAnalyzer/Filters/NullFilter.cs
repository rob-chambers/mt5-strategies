using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class NullFilter : Filter
    {
        public override string Name => "<None>";

        public override string Description => "No filter applied";

        public override bool IsCombinable => false;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return true;
        }
    }
}
