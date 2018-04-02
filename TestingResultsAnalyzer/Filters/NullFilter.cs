using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class NullFilter : Filter
    {
        public override string Name => "<None>";

        public override string Description => "Clears any existing filters";

        public override bool IsIncluded(TradeViewModel trade)
        {
            return true;
        }
    }
}
