using FileHelpers;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

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
                    InitialDirectory = @"C:\Users\rob\AppData\Roaming\MetaQuotes\Tester\D0E8209F77C8CF37AD8BF550E51FF075\Agent-127.0.0.1-3000\MQL5\Files",
                    Filter = "Comma Separated Values File (.csv)|*.csv",
                    Title = "Open backtesting results CSV file",
                    CheckFileExists = true
                };
                var result = dialog.ShowDialog();

                if (result == DialogResult.Cancel) return;

                var engine = new FileHelperEngine<Trade>
                {
                };

                _mainViewModel.Trades.Clear();
                var trades = engine.ReadFile(dialog.FileName).ToList();
                foreach (var trade in trades)
                {
                    _mainViewModel.Trades.Add(trade);
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
