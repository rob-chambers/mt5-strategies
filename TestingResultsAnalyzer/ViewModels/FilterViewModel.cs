using System;
using System.Linq;
using System.Windows.Input;
using TestingResultsAnalyzer.Commands;
using TestingResultsAnalyzer.Filters;

namespace TestingResultsAnalyzer.ViewModels
{
    public class FilterViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ShowFilteredTradesCommand _showFilteredTradesCommand;
        private PerformanceData _performanceData;
        private TradeCollection _trades;

        public FilterViewModel(MainViewModel mainViewModel, Filter filter)
        {
            _mainViewModel = mainViewModel;
            Filter = filter;
            _showFilteredTradesCommand = new ShowFilteredTradesCommand(mainViewModel);
            _showFilteredTradesCommand.Executed += ShowFilteredTrades;
        }        

        public Filter Filter { get; private set; }        

        public ICommand ShowFilteredTradesCommand
        {
            get
            {
                return _showFilteredTradesCommand;
            }
        }

        public PerformanceData PerformanceData
        {
            get
            {
                return _performanceData ?? (_performanceData = new PerformanceData());
            }
        }

        public void CalculateSummary(TradeCollection trades)
        {
            _trades = trades;
            var includedTrades = trades.Where(x => Filter.IsIncluded(x));

            PerformanceData.ProfitLoss = includedTrades.Sum(x => x.Profit);
            PerformanceData.MaxProfit = includedTrades.Any() ? includedTrades.Max(x => x.Profit) : 0;
            PerformanceData.MaxLoss = includedTrades.Any() ? -includedTrades.Min(x => x.Profit) : 0;
            PerformanceData.TotalTrades = includedTrades.Count();
            PerformanceData.TotalWins = includedTrades.Count(x => x.Profit > 0);
            PerformanceData.TotalLosses = includedTrades.Count(x => x.Profit <= 0);
            PerformanceData.WinLossRatio = PerformanceData.TotalTrades > 0
                ? (PerformanceData.TotalLosses == 0 ? 100 : (double)PerformanceData.TotalWins / PerformanceData.TotalLosses * 100)
                : 0;
        }

        private void ShowFilteredTrades(object source, EventArgs e)
        {
            foreach (var trade in _trades)
            {
                trade.IsSelected = Filter.IsIncluded(trade);
            }
        }
    }
}
