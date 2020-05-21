// Version 2020-05-21 15:32
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class MacdDivergence : Indicator
    {
        private const string SignalGroup = "Signal";
        private const string NotificationsGroup = "Notifications";

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 12, Group = SignalGroup)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 26, Group = SignalGroup)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Signal Periods", DefaultValue = 9, Group = SignalGroup)]
        public int SignalPeriods { get; set; }

        [Parameter("Send email alerts", DefaultValue = false, Group = NotificationsGroup)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = NotificationsGroup)]
        public bool PlayAlertSound { get; set; }

        [Parameter("Show alert message", DefaultValue = false, Group = NotificationsGroup)]
        public bool ShowMessage { get; set; }

        [Output("Signal", LineColor = "Red", LineStyle = LineStyle.LinesDots)]
        public IndicatorDataSeries Signal { get; set; }

        [Output("MACD", LineColor = "Blue", LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries MACD { get; set; }

        [Output("Histogram", IsHistogram = true, LineColor = "Cyan", LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries Histogram { get; set; }

        private MacdCrossOver _macdCrossOver;
        private int _latestSignalIndex;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _macdCrossOver = Indicators.MacdCrossOver(Source, SlowPeriodParameter, FastPeriodParameter, SignalPeriods);
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            Signal[index] = _macdCrossOver.Signal[index];
            MACD[index] = _macdCrossOver.MACD[index];
            Histogram[index] = _macdCrossOver.Histogram[index];

            if (IsBullishBar(index))
            {
                if (HasVeryRecentSignal(index))
                {
                    _latestSignalIndex = index;
                    return;
                }

                AddSignal(index);                
            }
        }

        private bool HasVeryRecentSignal(int index)
        {
            const int ThresholdForRecent = 3;

            var diff = index - _latestSignalIndex;
            return diff <= ThresholdForRecent;
        }

        private bool IsBullishBar(int index)
        {
            //if (Bars.ClosePrices[index] >= Bars.OpenPrices[index]) 
            //    return false;

            //if (Bars.ClosePrices[index] >= Bars.ClosePrices[index - 1]) 
            //    return false;

            if (_macdCrossOver.MACD.LastValue >= 0 || _macdCrossOver.Signal.LastValue >= 0)
                return false;

            if (!_macdCrossOver.MACD.HasCrossedAbove(_macdCrossOver.Signal, 1))
                return false;
  
            return true;
        }

        private void AddSignal(int index)
        {
            _latestSignalIndex = index;
            DrawBullishPoint(index);
            HandleAlerts();
        }

        private void DrawBullishPoint(int index)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.LowPrices[index] - diff;
            Chart.DrawIcon("macddiverg" + index, ChartIconType.UpTriangle, index, y, Color.LightBlue);
        }

        private double GetVerticalDrawingBuffer()
        {
            var diff = Chart.TopY - Chart.BottomY;
            diff /= 25;
            return diff;
        }

        private void HandleAlerts()
        {
            // Make sure the email will be sent only at RealTime
            if (IsBacktesting || !IsLastBar)
                return;

            if (SendEmailAlerts)
            {
                var subject = string.Format("MACD Divergence found on {0} {1}", 
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("macd-divergence@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");

            if (ShowMessage)
                AlertService.SendAlert(new Alert("MACD Divergence", Symbol.Name, Bars.TimeFrame.ToString()));
        }
    }
}
