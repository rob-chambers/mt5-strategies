using System;
using System.IO;
using System.Windows.Forms;

namespace cTrader.DesktopNotifications
{
    internal class FileWatchingService
    {
        private FileSystemWatcher _watcher;

        public event EventHandler<Alert> Alert;

        public void Watch(string path)
        {
            _watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.*"
            };

            _watcher.Created += new FileSystemEventHandler(OnCreated);
            _watcher.EnableRaisingEvents = true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            AddName(e.Name);
        }

        private void AddName(string fileName)
        {
            var parts = fileName.Split('_');
            if (parts.Length < 3)
            {
                return;
            }

            var alert = new Alert
            {
                TriggerTimeStamp = DateTime.Now,
                Pair = parts[0],
                TimeFrame = parts[1],
                Indicator = parts[2]
            };

            OnAlert(alert);
        }

        public void Stop()
        {
            if (_watcher == null)
            {
                return;
            }

            _watcher.EnableRaisingEvents = false;
        }

        protected void OnAlert(Alert alert)
        {
            if (Alert == null)
            {
                return;
            }

            Alert(this, alert);
        }
    }
}
