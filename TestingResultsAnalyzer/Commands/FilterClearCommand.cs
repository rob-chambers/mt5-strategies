using System;
using System.ComponentModel;
using System.Windows.Input;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class FilterClearCommand : ICommand
    {
        protected readonly MainViewModel _mainViewModel;

        public event EventHandler CanExecuteChanged;
        public event EventHandler Executed;

        public FilterClearCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        }

        private void OnMainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsEnabled))
            {
                if (CanExecuteChanged == null) return;
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _mainViewModel.IsEnabled;
        }

        public void Execute(object parameter)
        {
            _mainViewModel.TopTradesFilter = TopTradesFilter.Off;
        }
    }
}
