using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public abstract class Filter : ViewModelBase
    {
        private bool _isSelected;
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