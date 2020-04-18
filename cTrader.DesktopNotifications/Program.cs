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
            AddAlert(alert);
        }

        private static void AddAlert(Alert alert)
        {            
            _alertForm.SetAlert(alert);
        }

        private static void OnMainFormClosed(object sender, FormClosedEventArgs e)
        {
            _watcher.Stop();
        }
    }
}
