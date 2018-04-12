using System;

namespace TestingResultsAnalyzer.ViewModels
{
    public class PerformanceData : ViewModelBase
    {
        private double _profitLoss;
        private double _grossProfits;
        private double _grossLosses;
        private double _maxProfit;
        private double _maxLoss;
        private double _avgWin;
        private double _avgLoss;
        private int _totalTrades;
        private int _totalWins;
        private int _totalLosses;
        private double _winLossRatio;
        private double _profitFactor;
        private int _numberConsecutiveLosses;
        private TimeSpan _averageHoldingTime;

        public double ProfitLoss
        {
            get
            {
                return _profitLoss;
            }
            set
            {
                if (_profitLoss == value) return;
                _profitLoss = value;
                OnPropertyChanged(nameof(ProfitLoss));
            }
        }

        public double GrossProfits
        {
            get
            {
                return _grossProfits;
            }
            set
            {
                if (_grossProfits == value) return;
                _grossProfits = value;
                OnPropertyChanged(nameof(GrossProfits));
            }
        }

        public double GrossLosses
        {
            get
            {
                return _grossLosses;
            }
            set
            {
                if (_grossLosses == value) return;
                _grossLosses = value;
                OnPropertyChanged(nameof(GrossLosses));
            }
        }

        public double MaxProfit
        {
            get
            {
                return _maxProfit;
            }
            set
            {
                if (_maxProfit == value) return;
                _maxProfit = value;
                OnPropertyChanged(nameof(MaxProfit));
            }
        }

        public double MaxLoss
        {
            get
            {
                return _maxLoss;
            }
            set
            {
                if (_maxLoss == value) return;
                _maxLoss = value;
                OnPropertyChanged(nameof(MaxLoss));
            }
        }

        public double AverageWin
        {
            get
            {
                return _avgWin;
            }
            set
            {
                if (_avgWin == value) return;
                _avgWin = value;
                OnPropertyChanged(nameof(AverageWin));
            }
        }

        public double AverageLoss
        {
            get
            {
                return _avgLoss;
            }
            set
            {
                if (_avgLoss == value) return;
                _avgLoss = value;
                OnPropertyChanged(nameof(AverageLoss));
            }
        }

        public int TotalTrades
        {
            get
            {
                return _totalTrades;
            }
            set
            {
                if (_totalTrades == value) return;
                _totalTrades = value;
                OnPropertyChanged(nameof(TotalTrades));
                OnPropertyChanged(nameof(WinLossRatio));
            }
        }

        public int TotalWins
        {
            get
            {
                return _totalWins;
            }
            set
            {
                if (_totalWins == value) return;
                _totalWins = value;
                OnPropertyChanged(nameof(TotalWins));
                OnPropertyChanged(nameof(WinLossRatio));
            }
        }

        public int TotalLosses
        {
            get
            {
                return _totalLosses;
            }
            set
            {
                if (_totalLosses == value) return;
                _totalLosses = value;
                OnPropertyChanged(nameof(TotalLosses));
                OnPropertyChanged(nameof(WinLossRatio));
            }
        }

        public double WinLossRatio
        {
            get
            {
                return _winLossRatio;
            }
            set
            {
                if (_winLossRatio == value) return;
                _winLossRatio = value;
                OnPropertyChanged(nameof(WinLossRatio));
            }
        }

        public double ProfitFactor
        {
            get
            {
                return _profitFactor;
            }
            set
            {
                if (_profitFactor == value) return;
                _profitFactor = value;
                OnPropertyChanged(nameof(ProfitFactor));
            }
        }
        
        public int NumberConsecutiveLosses
        {
            get { return _numberConsecutiveLosses; }
            set
            {
                if (_numberConsecutiveLosses == value) return;
                _numberConsecutiveLosses = value;
                OnPropertyChanged(nameof(NumberConsecutiveLosses));
            }
        }
        
        public TimeSpan AverageHoldingTime
        {
            get { return _averageHoldingTime; }
            set
            {
                if (_averageHoldingTime == value) return;
                _averageHoldingTime = value;
                OnPropertyChanged(nameof(AverageHoldingTime));
            }
        }
    }
}
