using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TrendTestingResultsAnalyzer.Commands;
using TrendTestingResultsAnalyzer.Filters;

namespace TrendTestingResultsAnalyzer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isEnabled;
        private string _title;
        private FilterViewModel _selectedFilter;
        private string _filterMax;
        private TopTradesFilter _topTradesFilter;

        public MainViewModel()
        {
            OpenFileCommand = new OpenFileCommand(this);
            FilterBestTradesCommand = new FilterBestTradesCommand(this);
            FilterWorstTradesCommand = new FilterWorstTradesCommand(this);
            FilterClearCommand = new FilterClearCommand(this);

            Trades = new TradeCollection();
            Filters = new ObservableCollection<FilterViewModel>();
            Title = "Testing Results Analyzer";
            InitFilters();
            FilterMax = "10";
        }

        public TradeCollection Trades { get; }

        public ObservableCollection<FilterViewModel> Filters { get; }

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

        public FilterViewModel CombinationFilter { get; private set; }
        public FilterViewModel NullFilter { get; private set; }
        public OpenFileCommand OpenFileCommand { get; }

        public FilterBestTradesCommand FilterBestTradesCommand { get; }

        public FilterWorstTradesCommand FilterWorstTradesCommand { get; }

        public FilterClearCommand FilterClearCommand { get; }

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
                
        public TopTradesFilter TopTradesFilter
        {
            get { return _topTradesFilter; }
            set
            {
                if (_topTradesFilter == value) return;
                _topTradesFilter = value;
                OnPropertyChanged(nameof(TopTradesFilter));
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
        
        public string FilterMax
        {
            get { return _filterMax; }
            set
            {
                if (_filterMax == value) return;
                _filterMax = value;
                OnPropertyChanged(nameof(FilterMax));
            }
        }

        public void CalculateSummary()
        {
            NullFilter.CalculateSummary(Trades);
            foreach (var filter in Filters)
            {
                filter.CalculateSummary(Trades);
            }

            IsEnabled = true;
        }

        private void InitFilters()
        {
            NullFilter = new FilterViewModel(this, new NullFilter());
            var filters = new[]
            {
                new FilterViewModel(this, new AboveFiftyPeriodFilter()),
                new FilterViewModel(this, new TwoHundredFortyFilter()),
                new FilterViewModel(this, new DailyMAFilter()),
                new FilterViewModel(this, new RsiFilter()),
                new FilterViewModel(this, new MACDZeroFilter()),
                new FilterViewModel(this, new MACDValueFilter()),
                new FilterViewModel(this, new FarAwayFilter()),
                new FilterViewModel(this, new MacdSignalIndexFilter()),
                new FilterViewModel(this, new ChangingTrendFilter()),
                new FilterViewModel(this, new CandlePatternFilter()),
                new FilterViewModel(this, new DailyTrendFilter()),
                new FilterViewModel(this, new OpenBelowMAFilter()),
                new FilterViewModel(this, new HighestHigh20Filter()),
                new FilterViewModel(this, new LateFridayFilter())
            };

            foreach (var filter in filters)
            {
                Filters.Add(filter);
            }

            CombinationFilter = new FilterViewModel(this, new CombineFilter(filters));
            Filters.Insert(1, CombinationFilter);
            AttachEventHandlers();
        }

        private void AttachEventHandlers()
        {
            foreach (var filter in Filters)
            {
                filter.Filter.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Filter.IsChecked):
                    CombinationFilter.CalculateSummary(Trades);
                    break;

                case nameof(Filter.ArgumentValue):
                    var filter = sender as Filter;
                    if (filter != null)
                    {
                        // Which vm does this correspond with?
                        var vm = Filters.SingleOrDefault(x => x.Filter == filter);
                        if (vm != null)
                        {
                            vm.CalculateSummary(Trades);
                        }

                        // Update combination filter as well
                        CombinationFilter.CalculateSummary(Trades);
                    }

                    break;
            }
        }
    }
}
