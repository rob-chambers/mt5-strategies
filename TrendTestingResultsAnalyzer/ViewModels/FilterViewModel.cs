using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using TrendTestingResultsAnalyzer.Commands;
using TrendTestingResultsAnalyzer.Filters;

namespace TrendTestingResultsAnalyzer.ViewModels
{
    public class FilterViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ShowFilteredTradesCommand _showFilteredTradesCommand;
        private PerformanceData _performanceData;
        private PerformanceData _excludedPerformanceData;
        private ComparisonPerformanceData _comparisonPerformanceData;
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

        public ComparisonPerformanceData ComparisonPerformanceData
        {
            get
            {
                return _comparisonPerformanceData ?? (_comparisonPerformanceData = new ComparisonPerformanceData());
            }
        }

        public void CalculateSummary(TradeCollection trades)
        {
            _trades = trades;
            var includedTrades = trades.Where(x => Filter.IsIncluded(x));
            var excludedTrades = trades.Where(x => !Filter.IsIncluded(x));

            CalculateSummary(includedTrades, PerformanceData);
            CalculateSummary(excludedTrades, ExcludedPerformanceData);

            var basicStrategyPerformance = new PerformanceData();
            CalculateSummary(trades, basicStrategyPerformance);
            CalculateComparisonSummary(basicStrategyPerformance, PerformanceData);
        }

        private void CalculateComparisonSummary(PerformanceData basicStrategyPerformance, PerformanceData filteredStrategyPerformance)
        {
            if (basicStrategyPerformance.ProfitFactor != 0)
            {
                var value = Math.Abs((filteredStrategyPerformance.ProfitFactor - basicStrategyPerformance.ProfitFactor) / basicStrategyPerformance.ProfitFactor) * 100;
                var temp = filteredStrategyPerformance.ProfitFactor > basicStrategyPerformance.ProfitFactor ? "IMPROVED" : "MADE WORSE";

                ComparisonPerformanceData.ProfitFactor = $"{temp} by {value:0.0}%";
            }
            else
            {
                ComparisonPerformanceData.ProfitFactor = string.Empty;
            }
            
            if (basicStrategyPerformance.WinLossRatio != 0)
            {
                var value = Math.Abs((filteredStrategyPerformance.WinLossRatio - basicStrategyPerformance.WinLossRatio) / basicStrategyPerformance.WinLossRatio) * 100;
                var temp = filteredStrategyPerformance.WinLossRatio > basicStrategyPerformance.WinLossRatio ? "IMPROVED" : "MADE WORSE";
                ComparisonPerformanceData.WinLossRatio = $"{temp} by {value:0.0}%";
            }
            else
            {
                ComparisonPerformanceData.WinLossRatio = string.Empty;
            }

            ComparisonPerformanceData.LosingTradesEliminated = basicStrategyPerformance.TotalLosses - filteredStrategyPerformance.TotalLosses;
            ComparisonPerformanceData.WinningTradesEliminated = basicStrategyPerformance.TotalWins - filteredStrategyPerformance.TotalWins;

            if (ComparisonPerformanceData.WinningTradesEliminated != 0)
            {
                var value = (double)ComparisonPerformanceData.WinningTradesEliminated / ComparisonPerformanceData.LosingTradesEliminated * 100;
                ComparisonPerformanceData.EliminationRatio = $"{value:0.00}";
            }
            else
            {
                ComparisonPerformanceData.EliminationRatio = string.Empty;
            }

            ComparisonPerformanceData.LossesEliminated = -basicStrategyPerformance.GrossLosses - (-filteredStrategyPerformance.GrossLosses);
            ComparisonPerformanceData.WinningsEliminated = basicStrategyPerformance.GrossProfits - filteredStrategyPerformance.GrossProfits;
        }

        private void CalculateSummary(IEnumerable<TradeViewModel> trades, PerformanceData performanceData)
        {
            var hasTrades = trades.Any();
            var profitableTrades = trades.Where(x => x.Profit > 0).ToList();
            var losingTrades = trades.Where(x => x.Profit <= 0).ToList();

            performanceData.ProfitLoss = trades.Sum(x => x.Profit);
            performanceData.GrossProfits = profitableTrades.Sum(x => x.Profit);
            performanceData.GrossLosses = losingTrades.Sum(x => x.Profit);
            performanceData.MaxProfit = hasTrades ? trades.Max(x => x.Profit) : 0;
            performanceData.MaxLoss = hasTrades
                ? losingTrades.Any()
                    ? -losingTrades.Min(x => x.Profit)
                    : 0
                : 0;
            performanceData.AverageWin = hasTrades && profitableTrades.Any() ? profitableTrades.Select(x => x.Profit).Average() : 0;
            performanceData.AverageLoss = hasTrades && losingTrades.Any() ? losingTrades.Select(x => x.Profit).Average() : 0;
            performanceData.TotalTrades = trades.Count();
            performanceData.TotalWins = profitableTrades.Count();
            performanceData.TotalLosses = losingTrades.Count();
            performanceData.WinLossRatio = performanceData.TotalTrades > 0
                ? (performanceData.TotalLosses == 0 ? 100 : (double)performanceData.TotalWins / performanceData.TotalTrades * 100)
                : 0;
            performanceData.ProfitFactor = performanceData.GrossLosses != 0
                ? performanceData.GrossProfits / -performanceData.GrossLosses
                : 0;

            performanceData.NumberConsecutiveLosses = CalculateConsecutiveLosses(trades);
            performanceData.AverageHoldingTime = hasTrades
                ? TimeSpan.FromMinutes(trades.Average(x => x.HoldingTime.TotalMinutes))
                : TimeSpan.Zero;
        }

        private int CalculateConsecutiveLosses(IEnumerable<TradeViewModel> trades)
        {
            int count = 0;
            int maxCount = 0;
            
            foreach (var trade in trades)
            {
                if (trade.Profit <= 0)
                {
                    count++;
                }
                else
                {
                    if (count > maxCount)
                    {
                        maxCount = count;
                    }

                    count = 0;
                }
            }

            if (count > maxCount)
            {
                maxCount = count;
            }

            return maxCount;
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
