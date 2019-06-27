using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class KnockOutBar : Indicator
    {
        [Output("Up Point", LineColor = "Lime", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries UpPoint { get; set; }

        [Output("Down Point", LineColor = "Yellow", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries DownPoint { get; set; }

        [Parameter(DefaultValue = 3, MinValue = 2, MaxValue = 5)]
        public double KnockOutBarThreshhold { get; set; }

        //[Parameter(DefaultValue = 21, MinValue = 8, MaxValue = 89)]
        //public int FastMAPeriod { get; set; }

        //[Parameter(DefaultValue = 21, MinValue = 11, MaxValue = 34)]
        //public int H4Periods { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        private AverageTrueRange _atr;
        private double _buffer;
        private MovingAverage _fastMA;
        private ExponentialMovingAverage _slowMA;

        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            _buffer = Symbol.PipSize * 5;
            //_fastMA = Indicators.ExponentialMovingAverage(SourceSeries, FastMAPeriod);

            //var h4 = MarketData.GetSeries(TimeFrame.Hour4);
            //_slowMA = Indicators.ExponentialMovingAverage(h4.Close, H4Periods);
        }

        public override void Calculate(int index)
        {              
            if (IsBullishKnockOutBar(index))
            {
                //var y = MarketSeries.Low[index - 1] - Symbol.PipSize;
                //ChartObjects.DrawText(Symbol.Code + "L" + index,
                //    BullishSignalText, 
                //    index - 1, 
                //    y,
                //    VerticalAlignment.Bottom, 
                //    HorizontalAlignment.Center, 
                //    _bullishColour);
                DrawBullishPoint(index);
            }
            //else if (IsBearishKnockOutBar(index))
            //{
            //    //var y = MarketSeries.High[index - 1] + Symbol.PipSize;
            //    //ChartObjects.DrawText(Symbol.Code + "S" + index,
            //    //    BearishSignalText,
            //    //    index - 1,
            //    //    y,
            //    //    VerticalAlignment.Top,
            //    //    HorizontalAlignment.Center,
            //    //    _bearishColour);
            //    DrawBearishPoint(index);
            //}
        }

        private bool IsBullishKnockOutBar(int index)
        {
            try
            {
                var priorIndex = index - 1;
                var currentLow = MarketSeries.Low[index];
                var priorLow = MarketSeries.Low[priorIndex];
                var currentHigh = MarketSeries.High[index];
                var close = MarketSeries.Close[index];
                var priorClose = MarketSeries.Close[priorIndex];
                var currentOpen = MarketSeries.Open[index];

                //if (close > priorLow) return false;
                //if (!(priorClose > _fastMA.Result.Last(priorBarIndex) || MarketSeries.Close[priorBarIndex - 1] > _fastMA.Result.Last(priorBarIndex - 1))) return false;

                //if (priorClose < _slowMA.Result.Last(index - 1)) return false;

                // Check length of KO bar
                var range = currentHigh - currentLow;
                if (range < _atr.Result[priorIndex] * KnockOutBarThreshhold)
                {
                    return false;
                }

                //var highestHigh = 0.0;
                //for (var i = 1; i < 4; i++)
                //{
                //    var highAtIndex = MarketSeries.High[index - i];
                //    if (highAtIndex > highestHigh)
                //    {
                //        highestHigh = highAtIndex;
                //    }
                //}

                //if (MarketSeries.High[index] - _atr.Result.LastValue > highestHigh) return false;                    
                var lowestLow = double.MaxValue;
                for (var i = 1; i < 10; i++)
                {
                    var lowAtIndex = MarketSeries.Low[index - i];
                    if (lowAtIndex < lowestLow)
                    {
                        lowestLow = lowAtIndex;
                    }
                }

                if (MarketSeries.Low[index] > lowestLow)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Print("Exception occurred: {0}", ex);
                return false;
            }
        }

        private bool IsBearishKnockOutBar(int index)
        {
            var lastBarIndex = index - 1;
            var priorBarIndex = lastBarIndex - 1;
            var currentLow = MarketSeries.Low[lastBarIndex];
            var priorLow = MarketSeries.Low[priorBarIndex];
            var currentHigh = MarketSeries.High[lastBarIndex];
            var priorHigh = MarketSeries.High[priorBarIndex];
            var close = MarketSeries.Close[lastBarIndex];
            var currentOpen = MarketSeries.Open[lastBarIndex];

            if (!(currentHigh > priorHigh)) return false;


            var closeFromLow = close - currentLow;
            var openFromHigh = currentHigh - currentOpen;
            var range = currentHigh - currentLow;

            var body = Math.Abs(close - currentOpen);
            var longWick = currentHigh - Math.Max(close, currentOpen);

            if (body > (1 - KnockOutBarThreshhold) * longWick)
            {
                return false;
            }

            var shortWick = Math.Min(close, currentOpen) - currentLow;

            if (shortWick > (1 - KnockOutBarThreshhold) * (currentHigh - Math.Max(close, currentOpen)))
            {
                return false;
            }

            // Check length of pin bar
            if (range < _atr.Result[lastBarIndex])
            {
                Print("Range too low for bar on {0}: {1}, {2}", 
                    MarketSeries.OpenTime[index], range, _atr.Result[lastBarIndex]);
                return false;
            }

            return true;
        }

        private void DrawBullishPoint(int index)
        {            
            UpPoint[index] = MarketSeries.Low[index] - _buffer;
        }

        private void DrawBearishPoint(int index)
        {
            DownPoint[index] = MarketSeries.High[index] + _buffer;
        }
    }
}
