using System;
using System.Configuration;
using System.Windows.Forms;

namespace cTrader.DesktopNotifications
{
    static class Program
    {
        private static FileWatchingService _watcher;
        private static AlertForm _alertForm;

        [STAThread]
        static void Main()
        {
            var path = ConfigurationManager.AppSettings["filewatchingpath"];
            _watcher = new FileWatchingService();
            _watcher.Alert += OnFileWatchingServiceAlert;
            _watcher.Watch(path);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _alertForm = new AlertForm
            {
                TopMost = true
            };
            _alertForm.FormClosed += OnMainFormClosed;
            Application.Run(_alertForm);
        }

        private static void OnFileWatchingServiceAlert(object sender, Alert alert)
        {
            _alertForm.WindowState = FormWindowState.Normal;
            _alertForm.Show();
            _alertForm.Activate();

            var message = string.Format("{0} Alert on {1} {2}", alert.Indicator, alert.Pair, alert.TimeFrame);
            MessageBox.Show(message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        private static void OnMainFormClosed(object sender, FormClosedEventArgs e)
        {
            _watcher.Stop();
        }
    }
}
