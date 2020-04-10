// Version 2020-04-10 18:05
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
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Send email alerts", DefaultValue = true)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = true)]
        public bool PlayAlertSound { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal", LineColor = "White")]
        public IndicatorDataSeries DownSignal { get; set; }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private MovingAverage _higherTimeframeMA;
        private double _buffer;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);

            var higherSeries = MarketData.GetBars(TimeFrame.Hour);
            _higherTimeframeMA = Indicators.MovingAverage(higherSeries.ClosePrices, FastPeriodParameter, MovingAverageType.Exponential);

            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            UpSignal[index] = double.NaN;
            DownSignal[index] = double.NaN;

            if (IsBullishBar(index))
            {
                DrawBullishPoint(index);
                HandleAlerts(true);
            }
            else if (IsBearishBar(index))
            {
                DrawBearishPoint(index);
                HandleAlerts(false);
            }            
        }

        private bool IsBullishBar(int index)
        {
            if (Bars.ClosePrices[index] < _higherTimeframeMA.Result[index])
            {
                return false;
            }

            if (!AreMovingAveragesStackedBullishlyAtIndex(index))
            {
                return false;
            }

            if (!AreMovingAveragesStackedBullishlyAtIndex(index - 1))
            {
                return true;
            }

            return false;
        }

        private bool IsBearishBar(int index)
        {
            if (Bars.ClosePrices[index] > _higherTimeframeMA.Result[index])
            {
                return false;
            }

            if (!AreMovingAveragesStackedBearishlyAtIndex(index))
            {
                return false;
            }

            if (!AreMovingAveragesStackedBearishlyAtIndex(index - 1))
            {
                return true;
            }

            return false;
        }

        private bool HasCrossedAllMovingAverages(int index)
        {
            var currentLow = Bars.LowPrices[index];
            var currentHigh = Bars.HighPrices[index];

            if (!(currentHigh >= _slowMA.Result[index])) return false;
            if (!(currentHigh >= _mediumMA.Result[index])) return false;
            if (!(currentHigh >= _fastMA.Result[index])) return false;

            if (!(currentLow <= _fastMA.Result[index])) return false;
            if (!(currentLow <= _mediumMA.Result[index])) return false;
            if (!(currentLow <= _slowMA.Result[index])) return false;

            return true;
        }

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
        {
            return index >= SlowPeriodParameter && _fastMA.Result[index] > _mediumMA.Result[index] &&
                _mediumMA.Result[index] > _slowMA.Result[index] &&
                _fastMA.Result[index] > _slowMA.Result[index];
        }

        private bool AreMovingAveragesStackedBearishlyAtIndex(int index)
        {
            return index >= SlowPeriodParameter && _fastMA.Result[index] < _mediumMA.Result[index] &&
                _mediumMA.Result[index] < _slowMA.Result[index] &&
                _fastMA.Result[index] < _slowMA.Result[index];
        }


        private void DrawBullishPoint(int index)
        {
            UpSignal[index] = 1.0;
            var y = Bars.LowPrices[index] - _buffer;            
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.Lime);
        }

        private void DrawBearishPoint(int index)
        {
            DownSignal[index] = 1.0;
            var y = Bars.HighPrices[index] + _buffer;
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownArrow, index, y, Color.White);
        }

        private void HandleAlerts(bool isBullish)
        {
            // Make sure the email will be sent only at RealTime
            if (!IsLastBar)
                return;

            if (SendEmailAlerts)
            {
                var subject = string.Format("{0} MA Cross formed on {1} {2}", 
                    isBullish ? "Bullish" : "Bearish",
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("MACrossOver@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
            {
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");
            }
        }
    }
}
