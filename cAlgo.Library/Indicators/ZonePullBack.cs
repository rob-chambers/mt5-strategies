// Version 2020-12-30 14:11
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;
using System;

/*
 * Rules for new indicator:

- 
 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class ZonePullBack : Indicator
    {
        private const string SignalGroup = "Signal";
        private const string NotificationsGroup = "Notifications";

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Send email alerts", DefaultValue = false, Group = NotificationsGroup)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = NotificationsGroup)]
        public bool PlayAlertSound { get; set; }

        [Parameter("Show alert message", DefaultValue = false, Group = NotificationsGroup)]
        public bool ShowMessage { get; set; }       

        //[Output("Up Signal", LineColor = "Lime")]
        //public IndicatorDataSeries UpSignal { get; set; }


        private int _latestSignalIndex;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;

        protected override void Initialize()
        {
            try
            {
                // Initialize and create nested indicators
                Print("Initializing Zone PullBack indicator");

                Print("Running mode: {0}", RunningMode);
                Print("IsBackTesting: {0}", IsBacktesting);

                _fastMA = Indicators.MovingAverage(Source, 21, MovingAverageType.Exponential);
                _mediumMA = Indicators.MovingAverage(Source, 55, MovingAverageType.Exponential);
                _slowMA = Indicators.MovingAverage(Source, 89, MovingAverageType.Simple);
                _latestSignalIndex = 0;

                Print("Finished initializing");

                GoToTestDate();
            }
            catch (Exception ex)
            {
                Print("Failed initialization: {0}", ex);
                throw;
            }
        }

        private void GoToTestDate()
        {
            var date = new DateTime(2020, 11, 4);

            while (Bars.OpenTimes[0] > date)
                Bars.LoadMoreHistory();
            Chart.ScrollXTo(date);
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            //UpSignal[index] = double.NaN;

            if (IsBullishBar(index))
            {
                if (HasVeryRecentSignal(index))
                {
                    _latestSignalIndex = index;
                    return;
                }

                AddSignal(index, Bars.LowPrices[index]);                
            }
        }

        private bool HasVeryRecentSignal(int index)
        {
            const int ThresholdForRecent = 10;

            var diff = index - _latestSignalIndex;
            return diff <= ThresholdForRecent;
        }

        private bool IsBullishBar(int index)
        {
            if (Bars.ClosePrices[index] >= Bars.OpenPrices[index])
                return false;

            if (Bars.LowPrices[index] >= Bars.LowPrices[index - 1])
                return false;

            if (Bars.LowPrices[index] >= _mediumMA.Result[index])
                return false;

            if (!AreMovingAveragesStackedBullishlyAtIndex(index))
                return false;

            if (HasJustEnteredZone(index))
                return false;

            return true;
        }

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
        {
            return index >= 89 && _fastMA.Result[index] > _mediumMA.Result[index] &&
                _mediumMA.Result[index] > _slowMA.Result[index] &&
                _fastMA.Result[index] > _slowMA.Result[index];
        }

        private bool HasJustEnteredZone(int index)
        {
            for (int i = 1; i <= 3; i++)
            {
                if (Bars.ClosePrices[index - i] >= _fastMA.Result[index - i])
                    return true;
            }

            return false;
        }

        private bool DoublesEqual(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Symbol.PipSize;
        }

        private void AddSignal(int index, double value)
        {
            Print("Adding signal at index {0}:{1}", index, value);
            _latestSignalIndex = index;
            //UpSignal[index] = value;
            DrawBullishPoint(index);
            HandleAlerts();
        }

        private void DrawBullishPoint(int index)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.LowPrices[index] - diff;
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.CornflowerBlue);
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

            if (RunningMode != RunningMode.RealTime)
                return;

            if (SendEmailAlerts)
            {
                var subject = string.Format("'Pull back into the zone' signal formed on {0} {1}", 
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("zonepullback@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");

            if (ShowMessage)
                AlertService.SendAlert(new Alert("ZonePullBack", Symbol.Name, Bars.TimeFrame.ToString()));
        }
    }
}
