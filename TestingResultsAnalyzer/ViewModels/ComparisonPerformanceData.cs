namespace TestingResultsAnalyzer.ViewModels
{
    public class ComparisonPerformanceData : ViewModelBase
    {
        private string _winLossRatio;
        private string _profitFactor;
        private int _losingTradesEliminated;
        private int _winningTradesEliminated;
        private string _eliminationRatio;
        private double _lossesEliminated;
        private double _winningsEliminated;

        public string WinLossRatio
        {
            get { return _winLossRatio; }
            set
            {
                if (_winLossRatio == value) return;
                _winLossRatio = value;
                OnPropertyChanged(nameof(WinLossRatio));
            }
        }
        
        public string ProfitFactor
        {
            get { return _profitFactor; }
            set
            {
                if (_profitFactor == value) return;
                _profitFactor = value;
                OnPropertyChanged(nameof(ProfitFactor));
            }
        }
        
        public int LosingTradesEliminated
        {
            get { return _losingTradesEliminated; }
            set
            {
                if (_losingTradesEliminated == value) return;
                _losingTradesEliminated = value;
                OnPropertyChanged(nameof(LosingTradesEliminated));
            }
        }

        public int WinningTradesEliminated
        {
            get { return _winningTradesEliminated; }
            set
            {
                if (_winningTradesEliminated == value) return;
                _winningTradesEliminated = value;
                OnPropertyChanged(nameof(WinningTradesEliminated));
            }
        }
        
        public string EliminationRatio
        {
            get { return _eliminationRatio; }
            set
            {
                if (_eliminationRatio == value) return;
                _eliminationRatio = value;
                OnPropertyChanged(nameof(EliminationRatio));
            }
        }
        
        public double LossesEliminated
        {
            get { return _lossesEliminated; }
            set
            {
                if (_lossesEliminated == value) return;
                _lossesEliminated = value;
                OnPropertyChanged(nameof(LossesEliminated));
            }
        }        

        public double WinningsEliminated
        {
            get { return _winningsEliminated; }
            set
            {
                if (_winningsEliminated == value) return;
                _winningsEliminated = value;
                OnPropertyChanged(nameof(WinningsEliminated));
            }
        }        
    }
}
