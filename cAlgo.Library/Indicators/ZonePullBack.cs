// Version 2020-12-31 13:49
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

        [Parameter("MAs Range Filter", DefaultValue = false, Group = SignalGroup)]
        public bool MaRangeFilter { get; set; }

        [Parameter("Stacked MAs Filter", DefaultValue = false, Group = SignalGroup)]
        public bool StackedMasFilter { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }


        private int _latestSignalIndex;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private double _maRangeBuffer;

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
                _maRangeBuffer = Symbol.PipSize * 4;

                Print("Finished initializing");
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
            UpSignal[index] = double.NaN;

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

            if (!InMaRange(index))
                return false;

            if (!IsStackedMasFilter(index))
                return false;

            return true;
        }

        private bool IsStackedMasFilter(int index)
        {
            if (!StackedMasFilter)
                return true;

            // Ensure the signal was proceeded by an up-trend, and this is just a pull-back
            var bars = new int[] { 30, 60 };

            foreach (var barIndex in bars)
            {
                var i = index - barIndex;
                if (!AreMovingAveragesStackedBullishlyAtIndex(i))
                {
                    Print("StackedMasFilter: Setup rejected as MAs not stacked at {0}",
                        Bars.OpenTimes[i].ToLocalTime());
                    return false;
                }
            }

            return true;
        }

        private bool InMaRange(int index)
        {
            if (!MaRangeFilter) return true;

            for (var i = 3; i <= 10; i++)
            {
                var j = index - i;
                if (Bars.HighPrices[j] > _fastMA.Result[j] + _maRangeBuffer || 
                    Bars.LowPrices[j] < _mediumMA.Result[j] - _maRangeBuffer)
                {
                    Print("MA Range filter: Rejected setup ({0},{1},{2},{3},{4})",
                        i, Bars.HighPrices[j], Bars.LowPrices[j], _fastMA.Result[j], _mediumMA.Result[j]);
                    return false;
                }
            }

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
            for (var i = 1; i <= 3; i++)
            {
                if (Bars.ClosePrices[index - i] >= _fastMA.Result[index - i])
                    return true;
            }

            return false;
        }

        private void AddSignal(int index, double value)
        {
            Print("Adding signal at index {0}:{1}", index, value);
            _latestSignalIndex = index;
            UpSignal[index] = value;
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
