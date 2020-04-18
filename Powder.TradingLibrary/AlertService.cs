using System;
using System.IO;

namespace Powder.TradingLibrary
{
    public static class AlertService
    {
        private static readonly string AlertFilePath = @"D:\cTrader-Alerts";

        public static void SendAlert(Alert alert)
        {
            var currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff");
            var fileName = string.Format("{0}_{1}_{2}_{3}", alert.Pair, alert.TimeFrame, alert.Indicator, currentDateTime);
            var path = Path.Combine(AlertFilePath, fileName);

            File.Create(path);
        }
    }
}
