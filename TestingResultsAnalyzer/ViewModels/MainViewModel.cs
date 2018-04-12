using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
        private FilterViewModel _nullFilter;
        private FilterViewModel _combinationFilter;
        private string _title;
        private FilterViewModel _selectedFilter;

        public MainViewModel()
        {
            _openFileCommand = new OpenFileCommand(this);

            _trades = new TradeCollection();
            _filters = new ObservableCollection<FilterViewModel>();
            Title = "Testing Results Analyzer";
            InitFilters();            
        }

        public TradeCollection Trades { get => _trades; }

        public ObservableCollection<FilterViewModel> Filters { get => _filters; }

        public FilterViewModel SelectedFilter
        {
            get
            {
                return _selectedFilter;
            }
            set
            {
                if (_selectedFilter == value) return;
                _selectedFilter = value;
                OnPropertyChanged(nameof(SelectedFilter));
            }
        }

        public FilterViewModel CombinationFilter => _combinationFilter;

        public FilterViewModel NullFilter => _nullFilter;

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

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public void CalculateSummary()
        {
            _nullFilter.CalculateSummary(Trades);
            foreach (var filter in _filters)
            {
                filter.CalculateSummary(Trades);
            }

            IsEnabled = true;
        }

        private void InitFilters()
        {
            _nullFilter = new FilterViewModel(this, new NullFilter());
            var filters = new[]
            {
                new FilterViewModel(this, new AboveFiftyPeriodFilter()),
                new FilterViewModel(this, new TwoHundredFortyFilter()),
                new FilterViewModel(this, new H4MAFilter()),
                new FilterViewModel(this, new H4RsiFilter()),
                new FilterViewModel(this, new MACDZeroFilter()),
                new FilterViewModel(this, new MACDValueFilter()),
                new FilterViewModel(this, new PriceToMacdDivergenceFilter()),
                new FilterViewModel(this, new MacdDivergenceFilter()),
                new FilterViewModel(this, new MacdDivergenceCrossFilter()),
                new FilterViewModel(this, new FlatTrendFilter()),
                new FilterViewModel(this, new FarAwayFilter()),
                new FilterViewModel(this, new MacdSignalIndexFilter()),
                new FilterViewModel(this, new ADXFilter()),
                new FilterViewModel(this, new ChangingTrendFilter())
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
            switch (e.PropertyName)
            {
                case nameof(Filter.IsChecked):
                    _combinationFilter.CalculateSummary(Trades);
                    break;

                case nameof(Filter.ArgumentValue):
                    var filter = sender as Filter;
                    if (filter != null)
                    {
                        // Which vm does this correspond with?
                        var vm = _filters.SingleOrDefault(x => x.Filter == filter);
                        if (vm != null)
                        {
                            vm.CalculateSummary(Trades);
                        }

                        // Update combination filter as well
                        _combinationFilter.CalculateSummary(Trades);
                    }

                    break;
            }
        }
    }
}
