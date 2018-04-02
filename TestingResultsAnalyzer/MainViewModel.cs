﻿using System;
using System.ComponentModel;
using System.Linq;
using TestingResultsAnalyzer.Commands;

namespace TestingResultsAnalyzer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly OpenFileCommand _openFileCommand;
        private TradeCollection _trades;
        private double _profitLoss;
        private double _maxProfit;
        private double _maxLoss;
        private int _totalTrades;
        private int _totalWins;
        private int _totalLosses;
        private double _winLossRatio;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            _openFileCommand = new OpenFileCommand(this);            
            _trades = new TradeCollection();
        }
       
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public TradeCollection Trades { get => _trades; }

        public OpenFileCommand OpenFileCommand { get => _openFileCommand; }

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

        public void CalculateSummary()
        {
            ProfitLoss = Trades.Sum(x => x.Profit);
            MaxProfit = Trades.Any() ? Trades.Max(x => x.Profit) : 0;
            MaxLoss = Trades.Any() ? -Trades.Min(x => x.Profit) : 0;
            TotalTrades = Trades.Count;
            TotalWins = Trades.Count(x => x.Profit > 0);
            TotalLosses = Trades.Count(x => x.Profit <= 0);
            WinLossRatio = TotalTrades > 0 
                ? (TotalLosses == 0 ? 100 : (double)TotalWins / TotalLosses * 100) 
                : 0;
        }
    }
}
