// Version 2020-04-18 17:12
using cAlgo.API;
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

        [Output("Up Signal")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal")]
        public IndicatorDataSeries DownSignal { get; set; }

        private QualitativeQuantitativeE _qqeAdv;
        private double _buffer;
        private int _lastAlertBarIndex;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _qqeAdv = Indicators.GetIndicator<QualitativeQuantitativeE>(8);
            _buffer = Symbol.PipSize * 5;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            UpSignal[index] = double.NaN;
            DownSignal[index] = double.NaN;

            if (IsBullishBar(index))
            {
                // The cross occurred on the previous bar
                DrawBullishPoint(index - 1);
                HandleAlerts(true, index);
            }
            else if (IsBearishBar(index))
            {
                // The cross occurred on the previous bar
                DrawBearishPoint(index - 1);
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

        private void DrawBullishPoint(int index)
        {
            UpSignal[index] = 1.0;
            var y = Bars.LowPrices[index] - _buffer;            
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpTriangle, index, y, Color.SpringGreen);
        }

        private void DrawBearishPoint(int index)
        {
            DownSignal[index] = 1.0;
            var y = Bars.HighPrices[index] + _buffer;
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownTriangle, index, y, Color.Magenta);
        }

        private void HandleAlerts(bool isBullish, int index)
        {
            // Make sure the alert will only be triggered in Real Time and ensure we haven't triggered already because this is called every tick
            if (!IsLastBar || _lastAlertBarIndex == index)
                return;

            _lastAlertBarIndex = index;
            var subject = string.Format("{0} QMP Filter signal fired on {1} {2}",
                isBullish ? "Bullish" : "Bearish",
                Symbol.Name,
                Bars.TimeFrame);

            if (SendEmailAlerts)
            {
                Notifications.SendEmail("QMPFilter@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
            {
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");
            }

            if (ShowMessage)
            {
                AlertService.SendAlert(new Alert("QMP Filter", Symbol.Name, Bars.TimeFrame.ToString()));
            }
        }
    }
}
