using System.Windows.Forms;

namespace cTrader.DesktopNotifications
{
    internal static class UIUtils
    {
        public static bool DismissableWarning(string warningTitle, string warningMessage, string settingKey)
        {
            var hide = Properties.Settings.Default.PropertyValues[settingKey]?.PropertyValue;
            if (hide == null)
            {
                return ShowMessage(warningTitle, warningMessage, settingKey);
            }

            bool.TryParse(hide.ToString(), out var hideMessage);
            if (hideMessage)
            {
                // if the user has decided to hide the message, we assume consent.
                return true;
            }
            else
            {
                return ShowMessage(warningTitle, warningMessage, settingKey);
            }
        }

        private static bool ShowMessage(string warningTitle, string warningMessage, string settingKey)
        {
            if (Dialogs.DismissableConfirmationWindow.ShowMessage(warningTitle, warningMessage, out var permanentlyDismissed) == DialogResult.OK)
            {
                if (permanentlyDismissed)
                {
                    Properties.Settings.Default.PropertyValues[settingKey].PropertyValue = true;
                    Properties.Settings.Default.Save();
                }

                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
