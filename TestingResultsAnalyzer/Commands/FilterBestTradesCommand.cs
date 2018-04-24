using System;
using System.Collections.Generic;
using System.Linq;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class FilterBestTradesCommand : FilterTopTradesCommand
    {
        public FilterBestTradesCommand(MainViewModel mainViewModel) : base(mainViewModel)
        {
        }

        protected override IEnumerable<TradeViewModel> GetFilteredTrades(int limit)
        {
            return _mainViewModel.OriginalTrades
                .Where(x => x.Profit > 0)
                .OrderByDescending(x => x.Profit)
                .Take(limit);
        }
    }
}
