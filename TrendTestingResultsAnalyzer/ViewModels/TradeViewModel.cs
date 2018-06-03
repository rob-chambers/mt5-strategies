using System;
using System.ComponentModel;
using TrendTestingResultsAnalyzer.Model;

namespace TrendTestingResultsAnalyzer.ViewModels
{
    public class TradeViewModel : Trade, INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public TradeViewModel(Trade model)
        {
            DealNumber = model.DealNumber;
            Direction = model.Direction;
            EntryDateTime = model.EntryDateTime;
            EntryPrice = model.EntryPrice;
            ExitDateTime = model.ExitDateTime;
            ExitPrice = model.ExitPrice;
            Profit = model.Profit;

            Low = model.Low;
            High = model.High;
            Open = model.Open;
            Close = model.Close;
            MA50 = model.MA50;
            MA240 = model.MA240;

            Signal = model.Signal;
            MACD = model.MACD;

            UpIdx = model.UpIdx;
            DownIdx = model.DownIdx;
            High20 = model.High20;
            High25 = model.High25;
            DailyTrend = model.DailyTrend;
            RsiCurrent = model.RsiCurrent;
            RsiPrior = model.RsiPrior;
            DailyMA1 = model.DailyMA1;
            DailyMA2 = model.DailyMA2;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
        
        public TimeSpan HoldingTime
        {
            get
            {
                return ExitDateTime.Subtract(EntryDateTime);
            }
        }
    }
}
