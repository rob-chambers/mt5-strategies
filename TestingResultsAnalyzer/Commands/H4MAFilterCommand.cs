using System;
using System.Windows.Input;

namespace TestingResultsAnalyzer
{
    public class H4MAFilterCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly MainViewModel _mainViewModel;

        public H4MAFilterCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            foreach (var trade in _mainViewModel.Trades)
            {
                trade.IsSelected = trade.Direction == TradeDirection.Long 
                    ? trade.EntryPrice > trade.H4MA
                    : trade.EntryPrice < trade.H4MA;
            }

            _mainViewModel.CalculateSummary();
        }
    }
}