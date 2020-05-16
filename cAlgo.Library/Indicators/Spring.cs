// Version 2020-05-16 13:50
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;
using System;
using System.Collections.Generic;

/*
 * Rules for new indicator:

   Most recent swing high is lower than prior swing high
   Current (signal) bar is lower than most recent swing low
   Current bar's close must be lower than prior close
   Current bar's close must be lower than its open
   At point of both swing highs, medium MA should be above long MA
   Current bar must have a decent range - at least one ATR
   We must be at a x (default 60) period low
 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class Spring : Indicator
    {
        private const string SignalGroup = "Signal";
        private const string NotificationsGroup = "Notifications";

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89, Group = SignalGroup)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55, Group = SignalGroup)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21, Group = SignalGroup)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Send email alerts", DefaultValue = false, Group = NotificationsGroup)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = NotificationsGroup)]
        public bool PlayAlertSound { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Parameter("SignalBarRangeMultiplier", DefaultValue = 1, MinValue = 0.5, MaxValue = 5, Step = 0.1, Group = SignalGroup)]
        public double SignalBarRangeMultiplier { get; set; }

        [Parameter("MA Flat Filter", DefaultValue = true, Group = SignalGroup)]
        public bool MaFlatFilter { get; set; }

        [Parameter("Breakout Filter", DefaultValue = true, Group = SignalGroup)]
        public bool BreakoutFilter { get; set; }

        [Parameter("Min Bars For Lowest Low", DefaultValue = 60, MinValue = 10, MaxValue = 100, Step = 5, Group = SignalGroup)]
        public int MinimumBarsForLowestLow { get; set; }

        [Parameter("Swing High Strength", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = SignalGroup)]
        public int SwingHighStrength { get; set; }

        [Parameter("Big Move Filter", DefaultValue = true, Group = SignalGroup)]
        public bool BigMoveFilter { get; set; }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private SwingHighLow _swingHighLowIndicator;
        private AverageTrueRange _atr;
        private double _buffer;
        private int _latestSignalIndex;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _fastMA = Indicators.MovingAverage(Source, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(Source, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(Source, SlowPeriodParameter, MovingAverageType.Exponential);
            _swingHighLowIndicator = Indicators.GetIndicator<SwingHighLow>(Bars.HighPrices, Bars.LowPrices, SwingHighStrength);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            UpSignal[index] = double.NaN;

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
            const int ThresholdForRecent = 10;

            var diff = index - _latestSignalIndex;
            return diff <= ThresholdForRecent;
        }

        private bool IsBullishBar(int index)
        {
            if (Bars.ClosePrices[index] >= Bars.OpenPrices[index]) 
                return false;
            
            if (Bars.ClosePrices[index] >= Bars.ClosePrices[index - 1]) 
                return false;

            var diff = Bars.LowPrices[index - 1] - Bars.LowPrices[index];
            if (diff <= Symbol.PipSize * 2)
                // We want to see a minimum 2 pip difference between this low and the prior low
                return false;

            if (Bars.LowPrices[index] > Common.LowestLow(Bars.LowPrices, MinimumBarsForLowestLow))
                return false;

            var range = Bars.HighPrices[index] - Bars.LowPrices[index];
            if (range < _atr.Result[index] * SignalBarRangeMultiplier)
                return false;

            var highs = CheckForHighs(index);
            if (highs.Count != 2)
                return false;

            var priorHighIndex = highs.Pop();
            var recentHighIndex = highs.Pop();

            if (Source[recentHighIndex] >= Source[priorHighIndex])
                return false;

            if (!CheckForLows(index))
                return false;

            if (MaFlatFilter && !IsPriceFlat(index, priorHighIndex))
                return false;

            if (BreakoutFilter && IsBreakingDownwards(index))
                return false;

            if (BigMoveFilter && IsBigMove(index))
                return false;

            return true;
        }

        private bool CheckForLows(int index)
        {
            var recentSwingLow = double.NaN;
            double priorSwingLow = 0;

            for (var i = index; i > index - 100; i--)
            {
                var low = _swingHighLowIndicator.SwingLowPlot[i];
                if (double.IsNaN(low) || DoublesEqual(low, 0))
                {
                    continue;
                }

                if (double.IsNaN(recentSwingLow))
                {
                    recentSwingLow = low;
                }
                else if (DoublesEqual(recentSwingLow, low))
                {
                    continue;
                }
                else
                {
                    priorSwingLow = low;
                    break;
                }
            }

            var currentLow = Bars.LowPrices[index];
            if (!(currentLow < priorSwingLow && currentLow < recentSwingLow))
            {
                return false;
            }

            return true;
        }

        private bool DoublesEqual(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Symbol.PipSize;
        }

        private Stack<int> CheckForHighs(int index)
        {
            var i = index;
            var highs = new Stack<int>();
            var recentSwingHigh = _swingHighLowIndicator.SwingHighPlot[i];
            if (double.IsNaN(recentSwingHigh))
            {
                return highs;
            }

            highs.Push(i);
            double priorSwingHigh;
            i--;
            while (i > index - 100)
            {
                priorSwingHigh = _swingHighLowIndicator.SwingHighPlot[i];
                if (double.IsNaN(priorSwingHigh) || (recentSwingHigh - priorSwingHigh) > Symbol.PipSize)
                {
                    return highs;
                }

                // Are they the same?
                if (Math.Abs(priorSwingHigh - recentSwingHigh) <= Symbol.PipSize)
                {
                    // Give more time to find prior high
                    i--;
                }
                else
                {
                    // We must have found a lower high
                    highs.Push(i);
                    break;
                }
            }

            return highs;
        }

        private bool IsPriceFlat(int index, int priorHighIndex)
        {
            const int BarsToMeasure = 20;

            if (index < BarsToMeasure)
            {
                return false;
            }

            var diff = Math.Abs(_fastMA.Result[index] - _fastMA.Result[index - BarsToMeasure]);
            var firstTest = diff < _atr.Result.LastValue * 2;

            // What about the distance between the fast and slow back at the highs?
            diff = Math.Abs(_fastMA.Result[priorHighIndex] - _slowMA.Result[priorHighIndex]);
            var secondTest = diff < _atr.Result.LastValue * 2;

            return firstTest && secondTest;
        }

        private bool IsBreakingDownwards(int index)
        {
            var diff = _slowMA.Result[index] - _fastMA.Result[index];
            return diff > _atr.Result.LastValue * 3/4;
        }

        private bool IsBigMove(int index)
        {
            const int BarsToMeasure = 80;

            var highestHigh = Common.HighestHigh(Bars.HighPrices, BarsToMeasure);
            var lowestLow = Common.LowestLow(Bars.LowPrices, BarsToMeasure);
            var diff = highestHigh - lowestLow;

            var atr = _atr.Result.LastValue;
            if (diff > atr * 10)
            {
                var time = Bars.OpenTimes[index].ToLocalTime();
                Print("Filtering out signal at {0} as we had a bg move.  Highest high={1}, Lowest low={2}, Diff={3}, ATR={4}",
                    time, highestHigh, lowestLow, diff, atr);
                return true;
            }

            return false;
        }

        private void AddSignal(int index)
        {
            _latestSignalIndex = index;
            UpSignal[index] = 1.0;
            DrawBullishPoint(index);
            HandleAlerts();
        }

        private void DrawBullishPoint(int index)
        {            
            var y = Bars.LowPrices[index] - _buffer;            
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.Lime);
        }

        private void HandleAlerts()
        {
            // Make sure the email will be sent only at RealTime
            if (!IsLastBar)
                return;

            if (SendEmailAlerts)
            {
                var subject = string.Format("Spring formed on {0} {1}", 
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("spring@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
            {
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");
            }
        }
    }
}
