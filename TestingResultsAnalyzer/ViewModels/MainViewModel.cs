using System.Collections.ObjectModel;
using System.ComponentModel;
using TestingResultsAnalyzer.Commands;
using TestingResultsAnalyzer.Filters;

namespace TestingResultsAnalyzer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly TradeCollection _trades;
        private readonly ObservableCollection<FilterViewModel> _filters;
        private readonly OpenFileCommand _openFileCommand;
        private bool _isEnabled;
        private FilterViewModel _combinationFilter;

        public MainViewModel()
        {
            _openFileCommand = new OpenFileCommand(this);

            _trades = new TradeCollection();
            _filters = new ObservableCollection<FilterViewModel>();
            InitFilters();
        }

        public TradeCollection Trades { get => _trades; }

        public ObservableCollection<FilterViewModel> Filters { get => _filters; }

        public OpenFileCommand OpenFileCommand { get => _openFileCommand; }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public void CalculateSummary()
        {
            foreach (var filter in _filters)
            {
                filter.CalculateSummary(Trades);
            }

            IsEnabled = true;
        }

        private void InitFilters()
        {
            var filters = new[]
            {
                new FilterViewModel(this, new NullFilter()),
                new FilterViewModel(this, new AboveFiftyPeriodFilter()),
                new FilterViewModel(this, new BelowFiftyPeriodFilter()),
                new FilterViewModel(this, new TwoHundredFortyFilter()),
                new FilterViewModel(this, new H4MAFilter()),
                new FilterViewModel(this, new H4RsiFilter()),
                new FilterViewModel(this, new MACDZeroFilter()),
                new FilterViewModel(this, new PriceToMacdDivergenceFilter()),
                new FilterViewModel(this, new MacdDivergenceFilter()),
                new FilterViewModel(this, new MacdDivergenceCrossFilter()),
                new FilterViewModel(this, new FlatTrendFilter()),                
                new FilterViewModel(this, new FarAwayFilter()),
            };

            foreach (var filter in filters)
            {
                _filters.Add(filter);
            }

            _combinationFilter = new FilterViewModel(this, new CombineFilter(filters));
            _filters.Insert(1, _combinationFilter);
            AttachEventHandlers();
        }

        private void AttachEventHandlers()
        {
            foreach (var filter in _filters)
            {
                filter.Filter.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Filter.IsSelected)) return;
            _combinationFilter.CalculateSummary(Trades);
        }
    }
}
