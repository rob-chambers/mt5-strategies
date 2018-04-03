using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public abstract class Filter : ViewModelBase
    {
        private bool _isSelected;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool IsIncluded(TradeViewModel trade);

        public abstract bool IsCombinable { get; }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}