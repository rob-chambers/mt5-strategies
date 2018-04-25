using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TestingResultsAnalyzer.Comparers;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        private TopProfitComparer _topProfitComparer;
        private WorstLossesComparer _worstLossesComparer;
        private int _filterLimit;
        private List<TradeViewModel> _topTrades;
        private List<TradeViewModel> _worstTrades;
        private ListCollectionView _listCollectionView;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnMainWindowLoaded;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {           
            _mainViewModel = DataContext as MainViewModel;
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

            var view = CollectionViewSource.GetDefaultView(_mainViewModel.Trades);
            if (view == null) return;
            var listCollectionView = view as ListCollectionView;
            if (listCollectionView == null) return;
            _listCollectionView = listCollectionView;

            _topProfitComparer = new TopProfitComparer();
            _listCollectionView.CustomSort = _topProfitComparer;
            _worstLossesComparer = new WorstLossesComparer();

            listCollectionView.Filter = ShouldShow;

            tradeList.ItemsSource = listCollectionView;
        }

        private bool ShouldShow(object obj)
        {
            var trade = obj as TradeViewModel;
            if (trade == null) return true;

            if (_topTrades == null)
            {
                return true;
            }

            switch (_mainViewModel.TopTradesFilter)
            {
                case TopTradesFilter.Off:
                    return _mainViewModel.SelectedFilter == null
                        ? true
                        : _mainViewModel.SelectedFilter.Filter.IsIncluded(trade);

                case TopTradesFilter.Best:
                    return _mainViewModel.SelectedFilter == null
                        ? _topTrades.Select(x => x.DealNumber).Contains(trade.DealNumber)
                        : _topTrades.Select(x => x.DealNumber).Contains(trade.DealNumber) && _mainViewModel.SelectedFilter.Filter.IsIncluded(trade);

                case TopTradesFilter.Worst:
                    return _mainViewModel.SelectedFilter == null
                        ? _worstTrades.Select(x => x.DealNumber).Contains(trade.DealNumber)
                        : _worstTrades.Select(x => x.DealNumber).Contains(trade.DealNumber) && _mainViewModel.SelectedFilter.Filter.IsIncluded(trade);
            }

            return true;
        }

        private void OnMainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.TopTradesFilter):
                    IComparer comparer = _topProfitComparer;

                    if (_mainViewModel.TopTradesFilter == TopTradesFilter.Worst)
                    {
                        comparer = _worstLossesComparer;
                    }

                    _listCollectionView.CustomSort = comparer;
                    RefreshList();
                    break;

                case nameof(MainViewModel.SelectedFilter):
                    if (_mainViewModel.TopTradesFilter == TopTradesFilter.Off)
                    {
                        return;
                    }

                    RefreshList();
                    break;

                case nameof(MainViewModel.FilterMax):
                    if (_mainViewModel.TopTradesFilter == TopTradesFilter.Off)
                    {
                        return;
                    }

                    RefreshList();
                    break;
            }
        }

        private void RefreshList()
        {
            _filterLimit = GetFilterLimit();

            if (_mainViewModel.SelectedFilter == null)
            {
                _topTrades = _mainViewModel.Trades
                    .Where(x => x.Profit > 0)
                    .OrderByDescending(x => x.Profit)
                    .Take(_filterLimit)
                    .ToList();

                _worstTrades = _mainViewModel.Trades
                    .Where(x => x.Profit <= 0)
                    .OrderBy(x => x.Profit)
                    .Take(_filterLimit)
                    .ToList();
            }
            else
            {
                _topTrades = _mainViewModel.Trades
                    .Where(x => _mainViewModel.SelectedFilter.Filter.IsIncluded(x) && x.Profit > 0)
                    .OrderByDescending(x => x.Profit)
                    .Take(_filterLimit)
                    .ToList();

                _worstTrades = _mainViewModel.Trades
                    .Where(x => _mainViewModel.SelectedFilter.Filter.IsIncluded(x) && x.Profit <= 0)
                    .OrderBy(x => x.Profit)
                    .Take(_filterLimit)
                    .ToList();
            }

            // Change to top trades filter - refresh the view
            _listCollectionView.Refresh();
        }

        private int GetFilterLimit()
        {
            if (!int.TryParse(_mainViewModel.FilterMax, out int limit))
            {
                return 0;
            }

            return limit;
        }
    }
}
