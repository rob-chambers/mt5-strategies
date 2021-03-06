// Version 2020-05-17 20:24
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.FileSystem)]
    public class QMPFilter : Indicator
    {
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Send email alerts", DefaultValue = true)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = true)]
        public bool PlayAlertSound { get; set; }

        [Parameter("Show alert message", DefaultValue = true)]
        public bool ShowMessage { get; set; }

        [Parameter("UseSlowH4Filter", DefaultValue = true)]
        public bool UseSlowH4Filter { get; set; }

        [Parameter("UseFastH4Filter", DefaultValue = true)]
        public bool UseFastH4Filter { get; set; }

        [Output("Up Signal")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal")]
        public IndicatorDataSeries DownSignal { get; set; }

        private QualitativeQuantitativeE _qqeAdv;
        private int _lastAlertBarIndex;
        private MovingAverage _fastMA;
        private MovingAverage _slowMA;
        
        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _qqeAdv = Indicators.GetIndicator<QualitativeQuantitativeE>(8);
            var h4series = MarketData.GetSeries(TimeFrame.Hour4);
            //_h4Rsi = Indicators.RelativeStrengthIndex(h4series.Close, 14);

            if (IsFastMaFilterInUse)
                _fastMA = Indicators.MovingAverage(h4series.Close, 21, MovingAverageType.Exponential);

            if (IsSlowMaFilterInUse)
                _slowMA = Indicators.MovingAverage(h4series.Close, 89, MovingAverageType.Exponential);
        }

        private bool IsFastMaFilterInUse
        {
            get
            {
                return UseFastH4Filter && Bars.TimeFrame < TimeFrame.Hour4;
            }
        }

        private bool IsSlowMaFilterInUse
        {
            get
            {
                return UseSlowH4Filter && Bars.TimeFrame < TimeFrame.Hour4;
            }
        }

        public override void Calculate(int index)
        {
            UpSignal[index] = double.NaN;
            DownSignal[index] = double.NaN;

            if (IsBullishBar(index))
                HandleBullishSetUp(index);
            else if (IsBearishBar(index))
                HandleBearishSetup(index);
        }

        private void HandleBullishSetUp(int index)
        {
            if (HasVeryRecentSignal(index))
            {
                _lastAlertBarIndex = index;
                return;
            }

            var slowMaRule = false;
            var fastMaRule = false;

            if (IsSlowMaFilterInUse && Bars.ClosePrices[index] > _slowMA.Result[index])
                slowMaRule = true;

            if (IsFastMaFilterInUse && Bars.ClosePrices[index] > _fastMA.Result[index])
                fastMaRule = true;

            AddSignal(index, true, slowMaRule, fastMaRule);
        }

        private void HandleBearishSetup(int index)
        {
            if (HasVeryRecentSignal(index))
            {
                _lastAlertBarIndex = index;
                return;
            }

            var slowMaRule = false;
            var fastMaRule = false;

            if (IsSlowMaFilterInUse && Bars.ClosePrices[index] < _slowMA.Result[index])
                slowMaRule = true;

            if (IsFastMaFilterInUse && Bars.ClosePrices[index] < _fastMA.Result[index])
                fastMaRule = true;

            AddSignal(index, false, slowMaRule, fastMaRule);
        }

        private bool HasVeryRecentSignal(int index)
        {
            const int ThresholdForRecent = 3;

            var diff = index - _lastAlertBarIndex;
            return diff <= ThresholdForRecent;
        }

        private void AddSignal(int index, bool isBullish, bool slowMaRule, bool fastMaRule)
        {
            _lastAlertBarIndex = index;

            // The cross occurred on the previous bar
            var drawingIndex = index - 1;

            if (isBullish)
            {
                UpSignal[index] = 1.0;
                DrawBullishPoint(drawingIndex, slowMaRule, fastMaRule);
                HandleAlerts(true, index);
            }
            else
            {
                DownSignal[index] = 1.0;
                DrawBearishPoint(drawingIndex, slowMaRule, fastMaRule);
                HandleAlerts(false, index);
            }
        }

        private bool IsBullishBar(int index)
        {
            return index > 0 && 
                _qqeAdv.Result[index] > _qqeAdv.ResultS[index] &&
                _qqeAdv.Result[index - 1] <= _qqeAdv.ResultS[index - 1];
        }

        private bool IsBearishBar(int index)
        {
            return index > 0 &&
                _qqeAdv.Result[index] < _qqeAdv.ResultS[index] &&
                _qqeAdv.Result[index - 1] >= _qqeAdv.ResultS[index - 1];
        }

        private void DrawBullishPoint(int index, bool slowMaRule, bool fastMaRule)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.LowPrices[index] - diff;            
            Chart.DrawIcon("qmpbull" + index, ChartIconType.UpTriangle, index, y, Color.SpringGreen);

            if (slowMaRule)
            {
                y += diff / 2;
                Chart.DrawIcon("slowmasignal" + index, ChartIconType.Star, index, y, Color.Lime);
            }

            if (fastMaRule)
            {
                y += diff / 2;
                Chart.DrawText("fastma" + index, "*", index, y, Color.White);
            }
        }

        private void DrawBearishPoint(int index, bool slowMaRule, bool fastMaRule)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.HighPrices[index] + diff;
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownTriangle, index, y, Color.Magenta);

            if (slowMaRule)
            {
                y -= diff / 2;
                Chart.DrawIcon("slowmasignal" + index, ChartIconType.Star, index, y, Color.Magenta);
            }

            if (fastMaRule)
            {
                y -= diff / 2;
                Chart.DrawText("fastma" + index, "*", index, y, Color.White);
            }
        }

        private double GetVerticalDrawingBuffer()
        {
            var diff = Chart.TopY - Chart.BottomY;
            diff /= 25;
            return diff;
        }

        private void HandleAlerts(bool isBullish, int index)
        {
            // Make sure the alert will only be triggered in Real Time and ensure we haven't triggered already because this is called every tick
            if (!IsLastBar || _lastAlertBarIndex == index || IsBacktesting)
                return;

            var subject = string.Format("{0} QMP Filter signal fired on {1} {2}",
                isBullish ? "Bullish" : "Bearish",
                Symbol.Name,
                Bars.TimeFrame);

            if (SendEmailAlerts)
                Notifications.SendEmail("QMPFilter@indicators.com", "rechambers11@gmail.com", subject, string.Empty);

            if (PlayAlertSound)
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");

            if (ShowMessage)
                AlertService.SendAlert(new Alert("QMP Filter", Symbol.Name, Bars.TimeFrame.ToString()));
        }
    }
}
