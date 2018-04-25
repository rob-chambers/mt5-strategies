using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class FilterWorstTradesCommand : FilterTopTradesCommand
    {
        public FilterWorstTradesCommand(MainViewModel mainViewModel) : base(mainViewModel)
        {
        }

        protected override TopTradesFilter Type => TopTradesFilter.Worst;
    }
}
