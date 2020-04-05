using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class MacdPlatinum : Indicator
    {
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Output("MACD", LineColor = "RoyalBlue", PlotType = PlotType.Line, LineStyle = LineStyle.Dots)]
        public IndicatorDataSeries ZeroLagMacd { get; set; }

        [Output("Signal1", LineColor = "IndianRed", PlotType = PlotType.Line)]
        public IndicatorDataSeries Signal1 { get; set; }

        //[Output("Signal2", LineColor = "Green", PlotType = PlotType.Line)]
        public IndicatorDataSeries Signal2 { get; set; }

        [Parameter("Play alert sound", DefaultValue = true)]
        public bool PlayAlertSound { get; set; }

        [Output("Up Signal", LineColor = "Pink", PlotType = PlotType.Points, Thickness = 3)]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal", LineColor = "DodgerBlue", PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries DownSignal { get; set; }

        [Parameter("Short Period", DefaultValue = 8)]
        public int Short { get; set; }

        [Parameter("Long Period", DefaultValue = 26)]
        public int Long { get; set; }

        [Parameter("Signal", DefaultValue = 9)]
        public int Signal { get; set; }

        private ExponentialMovingAverage _short;
        private ExponentialMovingAverage _short2;
        private ExponentialMovingAverage _long;
        private ExponentialMovingAverage _long2;
        
        //private IndicatorDataSeries _zeroLagMACD;
        //private IndicatorDataSeries _signal1;
        //private IndicatorDataSeries _signal2;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _short = Indicators.ExponentialMovingAverage(SourceSeries, Short);
            _short2 = Indicators.ExponentialMovingAverage(_short.Result, Short);

            _long = Indicators.ExponentialMovingAverage(SourceSeries, Long);
            _long2 = Indicators.ExponentialMovingAverage(_long.Result, Long);

            //_zeroLagMACD = CreateDataSeries();
            //_signal1 = CreateDataSeries();
            //_signal2 = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            UpSignal[index] = double.NaN;
            DownSignal[index] = double.NaN;

            var differenceShort = _short.Result[index] - _short2.Result[index];
            var zeroLagShort = _short.Result[index] + differenceShort;

            var differenceLong = _long.Result[index] - _long2.Result[index];
            var zeroLagLong = _long.Result[index] + differenceLong;

            ZeroLagMacd[index] = zeroLagShort - zeroLagLong;

            Signal1[index] = Indicators.ExponentialMovingAverage(ZeroLagMacd, Signal).Result[index];
            Signal2[index] = Indicators.ExponentialMovingAverage(Signal1, Signal).Result[index];

            var difference2 = Signal1[index] - Signal2[index];
            var difference2Prev = Signal1[index - 1] - Signal2[index - 1];

            var signalMACD = Signal1[index] + difference2;
            var signalMACDPrev = Signal1[index - 1] + difference2Prev;

            if (signalMACD >= ZeroLagMacd[index])
            {
                UpSignal[index] = 1.0;
                IndicatorArea.DrawIcon("bearsignal" + index, ChartIconType.Circle, index, signalMACD, Color.DodgerBlue);
            }
            else if (signalMACD <= ZeroLagMacd[index])
            {
                DownSignal[index] = 1.0;
            }

            /*
             * if signalMACD&gt;=zerolagMACD then
             r=255
             g=69
             b=0
            else
             r=54
             g=224
             b=208
            endif

            drawbarchart(signalMACD,zerolagMACD,signalMACD,zerolagMACD) coloured(r,g,b)

            if zerolagMACD crosses over signalMACD or zerolagMACD crosses under signalMACD then
             drawtext("●",barindex,signalMACD,Dialog,Bold,12) coloured(r,g,b)
            endif
             */

        }

        private void DrawBullishPoint(int index)
        {
            UpSignal[index] = 1.0;
            var y = MarketSeries.Low[index];
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.Lime);
        }

        private void DrawBearishPoint(int index)
        {
            DownSignal[index] = 1.0;
            var y = MarketSeries.High[index];
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownArrow, index, y, Color.White);
        }

        private void HandleAlerts(bool isBullish)
        {
            // Make sure the email will be sent only at RealTime
            if (!IsLastBar)
                return;

            if (PlayAlertSound)
            {
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");
            }
        }
    }
}
