using System.ComponentModel;
using TestingResultsAnalyzer.Model;

namespace TestingResultsAnalyzer.ViewModels
{
    public class TradeViewModel : Trade, INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public TradeViewModel(Trade model)
        {
            DealNumber = model.DealNumber;
            Direction = model.Direction;
            EntryDateTime = model.EntryDateTime;
            EntryPrice = model.EntryPrice;
            ExitDateTime = model.ExitDateTime;
            ExitPrice = model.ExitPrice;
            H4MA = model.H4MA;
            H4MA1 = model.H4MA1;
            H4Rsi = model.H4Rsi;
            H4Rsi1 = model.H4Rsi1;
            MA100 = model.MA100;
            MA240 = model.MA240;
            MA50 = model.MA50;
            MACD = model.MACD;
            Profit = model.Profit;
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
