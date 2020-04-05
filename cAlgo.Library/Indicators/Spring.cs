using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Collections.Generic;

/*
 * Rules for new indicator:

   Most recent swing high is lower than prior swing high
   Current (signal) bar is lower than most recent swing low
   Current bar's close must be lower than prior close
   Current bar's close must be lower than its open
   At point of both swing highs, medium MA should be above long MA
 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class Spring : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Send email alerts", DefaultValue = false)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false)]
        public bool PlayAlertSound { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Parameter("Period", DefaultValue = 13, MinValue = 3)]
        public int Period { get; set; }

        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private double _buffer;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _mediumMA = Indicators.MovingAverage(Source, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(Source, SlowPeriodParameter, MovingAverageType.Exponential);
            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            UpSignal[index] = double.NaN;

            if (IsBullishBar(index))
            {
                DrawBullishPoint(index);
                HandleAlerts(true);
            }           
        }

        private bool IsBullishBar(int index)
        {
            if (Bars.ClosePrices[index] >= Bars.OpenPrices[index]) return false;
            if (Bars.ClosePrices[index] >= Bars.ClosePrices[index - 1]) return false;
            if (Bars.LowPrices[index] >= Bars.LowPrices[index - 1]) return false;

            var min = Math.Max(index - 50, 0);
            var i = index;
            var highs = new Stack<int>();

            do
            {                
                if (IsLocalExtremum(i, true))
                {
                    highs.Push(i);
                    if (highs.Count >= 2)
                    {
                        break;
                    }
                }

                i -= Period;
            } while (i > min);

            if (highs.Count != 2)
            {
                return false;
            }

            var priorHighIndex = highs.Pop();
            var recentHighIndex = highs.Pop();

            if (Source[recentHighIndex] >= Source[priorHighIndex])
            {
                return false;
            }

            if (!(AreMovingAveragesStackedBullishlyAtIndex(priorHighIndex) &&
                AreMovingAveragesStackedBullishlyAtIndex(recentHighIndex)))
            {
                return false;
            }

            var priorHighPrice = Source[priorHighIndex];
            var recentHighPrice = Source[recentHighIndex];

            var info = string.Format("I:{0},RH:{1},PH:{2}", index, recentHighIndex, priorHighIndex);

            Chart.DrawText("I" + index,
                info,
                index,
                Bars.ClosePrices[index] - _buffer,
                Color.Wheat);

            Chart.DrawText("PH" + priorHighIndex, 
                priorHighPrice.ToString("G"), 
                priorHighIndex, 
                priorHighPrice - _buffer, 
                Color.Pink);

            Chart.DrawText("RH" + recentHighIndex,
                recentHighPrice.ToString("G"),
                recentHighIndex,
                recentHighPrice - _buffer,
                Color.Aqua);

            return true;
        }

        private bool IsLocalExtremum(int index, bool findMax)
        {
            var end = Math.Min(index, Source.Count - 1);
            var start = Math.Max(index - Period, 0);

            var value = Bars.HighPrices[index];

            for (var i = start; i <= end; i++)
            {
                if (findMax && value < Bars.HighPrices[i])
                    return false;

                if (!findMax && value > Bars.HighPrices[i])
                    return false;
            }

            return true;
        }

        //private bool IsLocalExtremumM(int index2, bool findMin)
        //{
        //    var start = Math.Min(index2 + Period, Source.Count - 1);
        //    var end = Math.Max(index2 - Period, 0);

        //    var value = Source[index2];

        //    for (var i = start; i <= end; i++)
        //    {
        //        if (findMin && value < Source[i])
        //            return false;

        //        if (!findMin && value > Source[i])
        //            return false;
        //    }

        //    return true;
        //}

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
        {
            return index >= SlowPeriodParameter &&
                _mediumMA.Result[index] > _slowMA.Result[index];
        }

        private void DrawBullishPoint(int index)
        {
            UpSignal[index] = 1.0;
            var y = Bars.LowPrices[index] - _buffer;            
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.Lime);
        }

        private void HandleAlerts(bool isBullish)
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
