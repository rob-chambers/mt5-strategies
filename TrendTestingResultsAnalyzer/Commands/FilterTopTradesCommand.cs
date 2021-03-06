﻿using System;
using System.ComponentModel;
using System.Windows.Input;
using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Commands
{
    public abstract class FilterTopTradesCommand : ICommand
    {
        protected readonly MainViewModel _mainViewModel;

        public event EventHandler CanExecuteChanged;
        public event EventHandler Executed;

        public FilterTopTradesCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        }

        protected abstract TopTradesFilter Type
        {
            get;
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
            _mainViewModel.TopTradesFilter = Type;
        }
    }
}
