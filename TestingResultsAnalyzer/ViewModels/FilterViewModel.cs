using System;
using System.Collections.Generic;
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
        private PerformanceData _excludedPerformanceData;
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

        public PerformanceData ExcludedPerformanceData
        {
            get
            {
                return _excludedPerformanceData ?? (_excludedPerformanceData = new PerformanceData());
            }
        }

        public void CalculateSummary(TradeCollection trades)
        {
            _trades = trades;
            var includedTrades = trades.Where(x => Filter.IsIncluded(x));
            var excludedTrades = trades.Where(x => !Filter.IsIncluded(x));

            CalculateSummary(includedTrades, PerformanceData);
            CalculateSummary(excludedTrades, ExcludedPerformanceData);
        }

        private void CalculateSummary(IEnumerable<TradeViewModel> trades, PerformanceData performanceData)
        {
            var hasTrades = trades.Any();
            var profitableTrades = trades.Where(x => x.Profit > 0);
            var losingTrades = trades.Where(x => x.Profit <= 0);

            performanceData.ProfitLoss = trades.Sum(x => x.Profit);
            performanceData.GrossProfits = profitableTrades.Sum(x => x.Profit);
            performanceData.GrossLosses = losingTrades.Sum(x => x.Profit);
            performanceData.MaxProfit = hasTrades ? trades.Max(x => x.Profit) : 0;
            performanceData.MaxLoss = hasTrades ? -trades.Min(x => x.Profit) : 0;
            performanceData.AverageWin = hasTrades ? profitableTrades.Average(x => x.Profit) : 0;
            performanceData.AverageLoss = hasTrades ? losingTrades.Average(x => x.Profit) : 0;
            performanceData.TotalTrades = trades.Count();
            performanceData.TotalWins = profitableTrades.Count();
            performanceData.TotalLosses = losingTrades.Count();
            performanceData.WinLossRatio = performanceData.TotalTrades > 0
                ? (performanceData.TotalLosses == 0 ? 100 : (double)performanceData.TotalWins / performanceData.TotalTrades * 100)
                : 0;
            performanceData.ProfitFactor = performanceData.GrossLosses != 0
                ? performanceData.GrossProfits / -performanceData.GrossLosses
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
