// Version 2020-12-19 16:07
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;
using System;
using System.Collections.Generic;

/*
 * Rules for new indicator:

- 
 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class ResistenceBreak : Indicator
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

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

       

        [Parameter("Min Bars For Lowest Low", DefaultValue = 60, MinValue = 10, MaxValue = 100, Step = 5, Group = SignalGroup)]
        public int MinimumBarsForLowestLow { get; set; }

        [Parameter("Swing High Strength", DefaultValue = 5, MinValue = 1, MaxValue = 5, Group = SignalGroup)]
        public int SwingHighStrength { get; set; }


        private SwingHighLow _swingHighLowIndicator;
        private AverageTrueRange _atr;
        private int _latestSignalIndex;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            Print("Initializing Resistence Break indicator");

            _swingHighLowIndicator = Indicators.GetIndicator<SwingHighLow>(Bars.ClosePrices, Bars.ClosePrices, SwingHighStrength);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            Print("Finished initializing");

            GoToTestDate();
        }

        private void GoToTestDate()
        {
            var date = new DateTime(2020, 11, 16);

            while (Bars.OpenTimes[0] > date)
                Bars.LoadMoreHistory();
            Chart.ScrollXTo(date);
        }

        public override void Calculate(int index)
        {
            // Ignore for real data for now
            if (IsLastBar) return;

            // Calculate value at specified index
            UpSignal[index] = double.NaN;

            if (IsBullishBar(index))
            {
                // Print("Bullish - checking for recent signal");
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
            const int ThresholdForRecent = 10;

            var diff = index - _latestSignalIndex;
            return diff <= ThresholdForRecent;
        }

        private bool IsBullishBar(int index)
        {
            if (Bars.ClosePrices[index] <= Bars.OpenPrices[index]) 
                return false;
            
            if (Bars.ClosePrices[index] <= Bars.ClosePrices[index - 1]) 
                return false;

            var highsLows = GetSwingHighLows(index);
            var highs = highsLows.Item1;
            var lows = highsLows.Item2;
            if (highs.Count <= 4)
                return false;

            var averageRange = _atr.Result[index];

            // Get most recent swing high and compare previous swing highs with that
            Print("cur sw high: {0}, prev swing high: {1}", 
                _swingHighLowIndicator.SwingHighPlot[index],
                _swingHighLowIndicator.SwingHighPlot[index - 1]);

            var currentPrice = _swingHighLowIndicator.SwingHighPlot[index - 1];
            var max = currentPrice + averageRange;
            var min = currentPrice - averageRange;


            int strength = 0;
            foreach (var highIndex in highs)
            {
                var high = _swingHighLowIndicator.SwingHighPlot[highIndex];

                if (high < max && high > min)
                {
                    var date = Bars.OpenTimes[highIndex].ToLocalTime();
                    Print("At {0} - SH on {1}: {2}", 
                        Bars.OpenTimes[index].ToLocalTime(), 
                        date,
                        high);
                    strength++;
                }
            }

            // Check for support at this level
            foreach (var lowIndex in lows)
            {
                var low = _swingHighLowIndicator.SwingLowPlot[lowIndex];

                if (low < max && low > min)
                {
                    var date = Bars.OpenTimes[lowIndex].ToLocalTime();
                    Print("At {0} - SL on {1}: {2}",
                        Bars.OpenTimes[index].ToLocalTime(),
                        date,
                        low);
                    strength++;
                }
            }

            if (strength <= 4)
                return false;

            // Check for a break above this resistence
            var thisBarPrice = Bars.ClosePrices[index];
            if (thisBarPrice <= currentPrice)
                return false;

            Print("Date: {0}, Curr price: {1}, min: {2}, max: {3}, strength: {4}",
                Bars.OpenTimes[index].ToLocalTime(), currentPrice, min, max, strength);

            return true;
        }

        private bool DoublesEqual(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Symbol.PipSize;
        }

        private Tuple<List<int>, List<int>> GetSwingHighLows(int index)
        {
            var i = index;
            var highs = new List<int>();
            var lows = new List<int>();
            double priorSwingHigh = 0;
            double priorSwingLow = 0;

            i--;
            while (i > index - 150)
            {
                var high = _swingHighLowIndicator.SwingHighPlot[i];
                var low = _swingHighLowIndicator.SwingLowPlot[i];

                if (!double.IsNaN(high) && priorSwingHigh != high)
                {
                    highs.Add(i);
                    priorSwingHigh = high;
                }

                if (!double.IsNaN(low) && priorSwingLow != low)
                {
                    lows.Add(i);
                    priorSwingLow = low;
                }

                i--;
            }

            return new Tuple<List<int>, List<int>>(highs, lows);
        }

        private void AddSignal(int index)
        {
            Print("adding signal");
            _latestSignalIndex = index;
            UpSignal[index] = 1.0;
            DrawBullishPoint(index);
            HandleAlerts();
        }

        private void DrawBullishPoint(int index)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.LowPrices[index] - diff;
            Chart.DrawIcon("bullsignal" + index, ChartIconType.Diamond, index, y, Color.HotPink);
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
                var subject = string.Format("Spring formed on {0} {1}", 
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("spring@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");

            if (ShowMessage)
                AlertService.SendAlert(new Alert("Spring", Symbol.Name, Bars.TimeFrame.ToString()));
        }
    }
}
