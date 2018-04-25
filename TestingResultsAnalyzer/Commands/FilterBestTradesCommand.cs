using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class FilterBestTradesCommand : FilterTopTradesCommand
    {
        public FilterBestTradesCommand(MainViewModel mainViewModel) : base(mainViewModel)
        {
        }

        protected override TopTradesFilter Type => TopTradesFilter.Best;
    }
}
