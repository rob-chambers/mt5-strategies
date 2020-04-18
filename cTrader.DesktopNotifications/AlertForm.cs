using System.Windows.Forms;

namespace cTrader.DesktopNotifications
{
    public partial class AlertForm : Form
    {
        private bool _exiting;

        public AlertForm()
        {
            InitializeComponent();
        }

        private void AlertForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_exiting)
            {
                //behave like Windows Messenger and other systray-based programs, hijack exit for close and explain
                e.Cancel = true;
                Hide();
            }

            // assuming we didn't actually exit, reset exiting flag for next time
            if (e.Cancel)
            {
                _exiting = false;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            _exiting = true;
            Close();
        }

        private void notifyIcon1_DoubleClick(object sender, System.EventArgs e)
        {
            ToggleLogWindowDisplay();
        }

        private void ToggleLogWindowDisplay()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }
            }
        }
    }
}
