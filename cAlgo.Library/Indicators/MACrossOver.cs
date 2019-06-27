using cAlgo.API;
using cAlgo.API.Indicators;

/*
 * Rules for new indicator:

We must have had a cross of all 3 MAs

For short:
- High must be within a couple of pips of 21MA
- Low must be within a couple of pips of 89MA
- Low must be lower than 55MA
- Close must be lower than open

    
- Futher Recent high (say 20 bars) must be > 50 pips higher than current high


*** Further filters
For longs:

Make sure we *close* above the short MA!
Has the short term MA crossed below the medium MA in the last x bars?
Ensure the RSI hasn't gone overbought

    

Over last 20 bars, what was the max distance (short - long) between the MAs?  If > 10 pips, filter out


    A stronger signal is made if we have a new low/high over x bars
    A stronger signal is made when the fast MA < either slow MA or medium MA in last x bars (say x = 5) (for a long)
    
    
*** Rules
Enter using a stop order in the same direction about half way above/below the range of the signal bar, unless it's a very significant bar


 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class MACrossOver : Indicator
    {
        [Output("Up Point", LineColor = "Lime", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries UpPoint { get; set; }

        [Output("Down Point", LineColor = "White", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries DownPoint { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private double _buffer;

        // REC Was here
        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            if (IsBullishPinBar(index))
            {
                DrawBullishPoint(index);
            }
            else if (IsBearishPinBar(index))
            {
                DrawBearishPoint(index);
            }            
        }

        private bool IsBullishPinBar(int index)
        {
            var lastBarIndex = index; // - 1;
            var open = MarketSeries.Open[lastBarIndex];
            var close = MarketSeries.Close[lastBarIndex];            

            if (!(close > open)) return false;

            var cross = HasCrossedAllMovingAverages(lastBarIndex);
            return cross && close > _fastMA.Result[index];
        }

        private bool IsBearishPinBar(int index)
        {
            var lastBarIndex = index; // - 1;
            var open = MarketSeries.Open[lastBarIndex];
            var close = MarketSeries.Close[lastBarIndex];

            if (!(open > close)) return false;

            var cross = HasCrossedAllMovingAverages(lastBarIndex);
            return cross && close < _fastMA.Result[index];
        }

        private bool HasCrossedAllMovingAverages(int index)
        {
            var currentLow = MarketSeries.Low[index];
            var currentHigh = MarketSeries.High[index];

            if (!(currentHigh >= _slowMA.Result[index])) return false;
            if (!(currentHigh >= _mediumMA.Result[index])) return false;
            if (!(currentHigh >= _fastMA.Result[index])) return false;

            if (!(currentLow <= _fastMA.Result[index])) return false;
            if (!(currentLow <= _mediumMA.Result[index])) return false;
            if (!(currentLow <= _slowMA.Result[index])) return false;

            return true;
        }

        private void DrawBullishPoint(int index)
        {
            UpPoint[index] = MarketSeries.Low[index] - _buffer;
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, UpPoint[index], Color.Lime);
        }

        private void DrawBearishPoint(int index)
        {
            DownPoint[index] = MarketSeries.High[index] + _buffer;
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownArrow, index, DownPoint[index], Color.White);
        }
    }
}
