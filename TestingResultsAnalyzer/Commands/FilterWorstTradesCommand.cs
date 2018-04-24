using System;
using System.Collections.Generic;
using System.Linq;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class FilterWorstTradesCommand : FilterTopTradesCommand
    {
        public FilterWorstTradesCommand(MainViewModel mainViewModel) : base(mainViewModel)
        {
        }

        protected override IEnumerable<TradeViewModel> GetFilteredTrades(int limit)
        {
            return _mainViewModel.OriginalTrades
                .Where(x => x.Profit <= 0)
                .OrderBy(x => x.Profit)
                .Take(limit);
        }
    }
}
