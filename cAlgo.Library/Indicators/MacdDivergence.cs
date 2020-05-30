// Version 2020-05-21 18:01
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Powder.TradingLibrary;
using System;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class MacdDivergence : Indicator
    {
        private const string SignalGroup = "Signal";
        private const string NotificationsGroup = "Notifications";

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 12, Group = SignalGroup)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 26, Group = SignalGroup)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Signal Periods", DefaultValue = 9, Group = SignalGroup)]
        public int SignalPeriods { get; set; }

        [Parameter("Send email alerts", DefaultValue = false, Group = NotificationsGroup)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = NotificationsGroup)]
        public bool PlayAlertSound { get; set; }

        [Parameter("Show alert message", DefaultValue = false, Group = NotificationsGroup)]
        public bool ShowMessage { get; set; }

        [Output("Signal", LineColor = "Red", LineStyle = LineStyle.LinesDots)]
        public IndicatorDataSeries Signal { get; set; }

        [Output("MACD", LineColor = "Blue", LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries MACD { get; set; }

        [Output("Histogram", IsHistogram = true, LineColor = "Cyan", LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries Histogram { get; set; }

        private MacdCrossOver _macdCrossOver;
        private int _latestSignalIndex;
        private int _priorBullishCrossIndex;
        private int _priorBearishCrossIndex;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            _macdCrossOver = Indicators.MacdCrossOver(Source, SlowPeriodParameter, FastPeriodParameter, SignalPeriods);
            _priorBullishCrossIndex = 0;
            _priorBearishCrossIndex = 0;
        }

        public override void Calculate(int index)
        {
            // Ignore for real data for now
            if (IsLastBar) return;

            // Calculate value at specified index
            Signal[index] = _macdCrossOver.Signal[index];
            MACD[index] = _macdCrossOver.MACD[index];
            Histogram[index] = _macdCrossOver.Histogram[index];

            if (IsBullishBar(index))
            {
                if (HasVeryRecentSignal(index))
                {
                    _latestSignalIndex = index;
                    return;
                }

                AddSignal(index, true);                
            }
            else if (IsBearishBar(index))
            {
                if (HasVeryRecentSignal(index))
                {
                    _latestSignalIndex = index;
                    return;
                }

                AddSignal(index, false);
            }
        }

        private bool HasVeryRecentSignal(int index)
        {
            const int ThresholdForRecent = 3;

            var diff = index - _latestSignalIndex;
            return diff <= ThresholdForRecent;
        }

        private bool IsBullishBar(int index)
        {
            if (!_macdCrossOver.MACD.HasCrossedAbove(_macdCrossOver.Signal, 1))
                return false;

            if (_macdCrossOver.MACD[index] >= 0 || _macdCrossOver.Signal[index] >= 0)
                return false;

            // We've had a cross - reset bearish flag
            _priorBearishCrossIndex = 0;

            // Ensure it's a decent cross - not just a touch
            if (IsTouch(index))
                return false;

            var priorMacd = _macdCrossOver.MACD[_priorBullishCrossIndex];
            var currentMacd = _macdCrossOver.MACD[index];
            if (_priorBullishCrossIndex == 0)
            {
                _priorBullishCrossIndex = index;
            }
            else if (index > _priorBullishCrossIndex && currentMacd > priorMacd)
            {
                var priorPrice = Bars.ClosePrices[_priorBullishCrossIndex];
                var currentPrice = Bars.ClosePrices[index];
                if (priorPrice >= currentPrice)
                {
                    Print("At {0}, prior MACD={1}, current MACD={2}, prior price={3}, current price={4}",
                        Bars.OpenTimes[index].ToLocalTime(), priorMacd, currentMacd, priorPrice, currentPrice);

                    var lowest = -_macdCrossOver.MACD.Minimum(100);
                    var ratio = lowest / -currentMacd;

                    if (ratio < 1.2)
                    {
                        Print("At {0}, rejecting due to divergence not being strong enough.  Ratio={1}",
                            Bars.OpenTimes[index].ToLocalTime(), ratio);

                        return false;
                    }

                    Print("At {0}, index={1}, prior={2}",
                        Bars.OpenTimes[index].ToLocalTime(), index, _priorBullishCrossIndex);

                    return true;
                }
            }

            return false;
        }

        private bool IsTouch(int index)
        {
            var diff = Math.Abs(_macdCrossOver.MACD[index] - _macdCrossOver.Signal[index]);
            return diff <= Symbol.PipSize;
        }

        private bool IsBearishBar(int index)
        {
            if (!_macdCrossOver.MACD.HasCrossedBelow(_macdCrossOver.Signal, 1))
                return false;

            if (_macdCrossOver.MACD[index] <= 0 || _macdCrossOver.Signal[index] <= 0)
                return false;

            // We've had a cross - reset bullish flag
            Print("At {0}, resetting bullish cross index to 0",
               Bars.OpenTimes[index].ToLocalTime());

            // Ensure it's a decent cross - not just a touch
            if (IsTouch(index))
                return false;

            if (_priorBearishCrossIndex == 0)
            {
                _priorBearishCrossIndex = index;
            }
            else if (index > _priorBearishCrossIndex && _macdCrossOver.MACD[index] < _macdCrossOver.MACD[_priorBearishCrossIndex])
            {
                if (Bars.ClosePrices[_priorBearishCrossIndex] <= Bars.ClosePrices[index])
                {
                    return true;
                }
            }

            return false;
        }

        private void AddSignal(int index, bool isBullish)
        {
            _latestSignalIndex = index;
            if (isBullish)
                DrawBullishPoint(index);
            else
                DrawBearishPoint(index);

            HandleAlerts();
        }

        private void DrawBullishPoint(int index)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.LowPrices[index] - diff;
            Chart.DrawIcon("macddiverg-bull" + index, ChartIconType.UpTriangle, index, y, Color.LightBlue);
        }

        private void DrawBearishPoint(int index)
        {
            var diff = GetVerticalDrawingBuffer();
            var y = Bars.HighPrices[index] + diff;
            Chart.DrawIcon("macddiverg-bear" + index, ChartIconType.DownTriangle, index, y, Color.OrangeRed);
        }

        private double GetVerticalDrawingBuffer()
        {
            var diff = Chart.TopY - Chart.BottomY;
            diff /= 25;
            return diff;
        }

        private void HandleAlerts()
        {
            // Make sure the email will be sent only at RealTime
            if (IsBacktesting || !IsLastBar)
                return;

            if (SendEmailAlerts)
            {
                var subject = string.Format("MACD Divergence found on {0} {1}", 
                    Symbol.Name,
                    Bars.TimeFrame);

                Notifications.SendEmail("macd-divergence@indicators.com", "rechambers11@gmail.com", subject, string.Empty);
            }

            if (PlayAlertSound)
                Notifications.PlaySound(@"c:\windows\media\ring03.wav");

            if (ShowMessage)
                AlertService.SendAlert(new Alert("MACD Divergence", Symbol.Name, Bars.TimeFrame.ToString()));
        }
    }
}
