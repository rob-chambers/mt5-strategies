namespace TestingResultsAnalyzer.ViewModels
{
    public class PerformanceData : ViewModelBase
    {
        private double _profitLoss;
        private double _maxProfit;
        private double _maxLoss;
        private int _totalTrades;
        private int _totalWins;
        private int _totalLosses;
        private double _winLossRatio;

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
    }
}
