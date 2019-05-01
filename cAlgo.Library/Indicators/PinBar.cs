using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class PinBar : Indicator
    {
        [Output("Up Point", LineColor = "Lime", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries UpPoint { get; set; }

        [Output("Down Point", LineColor = "Yellow", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries DownPoint { get; set; }

        [Parameter(DefaultValue = 0.67, MinValue = 0.5, MaxValue = 0.95)]
        public double PinbarThreshhold { get; set; }

        [Parameter(DefaultValue = "­­­·")]
        public string BullishSignalText { get; set; }

        [Parameter(DefaultValue = "·")]
        public string BearishSignalText { get; set; }

        private AverageTrueRange _atr;
        private double _buffer;

        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(7, MovingAverageType.Exponential);
            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {              
            if (IsBullishPinBar(index))
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
            else if (IsBearishPinBar(index))
            {
                //var y = MarketSeries.High[index - 1] + Symbol.PipSize;
                //ChartObjects.DrawText(Symbol.Code + "S" + index,
                //    BearishSignalText,
                //    index - 1,
                //    y,
                //    VerticalAlignment.Top,
                //    HorizontalAlignment.Center,
                //    _bearishColour);
                DrawBearishPoint(index);
            }
        }

        private bool IsBullishPinBar(int index)
        {
            var lastBarIndex = index - 1;
            var priorBarIndex = lastBarIndex - 1;
            var currentLow = MarketSeries.Low[lastBarIndex];
            var priorLow = MarketSeries.Low[priorBarIndex];
            var currentHigh = MarketSeries.High[lastBarIndex];
            var priorHigh = MarketSeries.High[priorBarIndex];
            var close = MarketSeries.Close[lastBarIndex];
            var currentOpen = MarketSeries.Open[lastBarIndex];

            if (!(currentLow < priorLow)) return false;
            if (!(close > priorLow)) return false;
            if (currentHigh > priorHigh) return false;

            var closeFromHigh = currentHigh - close;
            var openFromHigh = currentHigh - currentOpen;
            var range = currentHigh - currentLow;

            if (!((closeFromHigh / range <= (1 - PinbarThreshhold)) &&
                (openFromHigh / range <= (1 - PinbarThreshhold))))
            {
                return false;
            }

            // Check length of pin bar
            if (range < _atr.Result[lastBarIndex])
            {
                return false;
            }

            return true;
        }

        private bool IsBearishPinBar(int index)
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
            //if (!(close < priorHigh)) return false;
            //if (currentLow < priorLow) return false;

            var closeFromLow = close - currentLow;
            var openFromHigh = currentHigh - currentOpen;
            var range = currentHigh - currentLow;

            var body = Math.Abs(close - currentOpen);
            var longWick = currentHigh - Math.Max(close, currentOpen);

            if (body > (1 - PinbarThreshhold) * longWick)
            {
                return false;
            }

            var shortWick = Math.Min(close, currentOpen) - currentLow;

            if (shortWick > (1 - PinbarThreshhold) * (currentHigh - Math.Max(close, currentOpen)))
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
            UpPoint[index - 1] = MarketSeries.Low[index - 1] - _buffer;
        }

        private void DrawBearishPoint(int index)
        {
            DownPoint[index - 1] = MarketSeries.High[index - 1] + _buffer;
        }
    }
}
