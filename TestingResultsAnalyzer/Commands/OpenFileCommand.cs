using FileHelpers;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using TestingResultsAnalyzer.Model;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Commands
{
    public class OpenFileCommand : ICommand
    {
        private readonly MainViewModel _mainViewModel;

        public OpenFileCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    InitialDirectory = @"C:\Users\rob\Documents\Trading\Forex Backtesting Results",
                    Filter = "Comma Separated Values File (.csv)|*.csv",
                    Title = "Open backtesting results CSV file",
                    CheckFileExists = true
                };
                var result = dialog.ShowDialog();

                if (result == DialogResult.Cancel) return;

                var engine = new FileHelperEngine<Trade>();

                _mainViewModel.Trades.Clear();
                var trades = engine.ReadFile(dialog.FileName).ToList();
                string fileName = dialog.FileName;
                _mainViewModel.Title = $"Testing Results Analyzer ({fileName})";

                foreach (var trade in trades)
                {
                    _mainViewModel.Trades.Add(new TradeViewModel(trade));
                }

                _mainViewModel.OriginalTrades.Clear();
                foreach (var trade in trades)
                {
                    _mainViewModel.OriginalTrades.Add(new TradeViewModel(trade));
                }

                _mainViewModel.CalculateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}
