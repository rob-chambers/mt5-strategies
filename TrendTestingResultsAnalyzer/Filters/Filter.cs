using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public abstract class Filter : ViewModelBase
    {
        private bool _isChecked;
        private string _argumentValue;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool IsIncluded(TradeViewModel trade);

        public abstract bool IsCombinable { get; }

        public virtual bool HasArgument => false;

        public string ArgumentValue
        {
            get
            {
                return _argumentValue;
            }
            set
            {
                if (_argumentValue == value) return;
                _argumentValue = value;
                OnPropertyChanged(nameof(ArgumentValue));
            }
        }

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }
}