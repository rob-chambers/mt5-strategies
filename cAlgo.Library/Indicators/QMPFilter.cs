// Version 2020-04-11 14:14
using cAlgo.API;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class QMPFilter : Indicator
    {
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Send email alerts", DefaultValue = true)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = true)]
        public bool PlayAlertSound { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal", LineColor = "White")]
        public IndicatorDataSeries DownSignal { get; set; }

        private QualitativeQuantitativeE _qqeAdv;
        private double _buffer;

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
                HandleAlerts(true);
            }
            else if (IsBearishBar(index))
            {
                // The cross occurred on the previous bar
                DrawBearishPoint(index - 1);
                HandleAlerts(false);
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
