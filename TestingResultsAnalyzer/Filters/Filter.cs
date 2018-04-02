using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public abstract class Filter
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool IsIncluded(TradeViewModel trade);
    }
}