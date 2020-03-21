using cAlgo.API;
using cAlgo.API.Indicators;
using System.Diagnostics;
using System.Linq;

/*
 * Rules for new indicator:
 * We must have had a recent cross into the overbought / oversold area of the RSI (e.g. < 30)
 * For a buy we must have a red bar followed by a green bar that closes higher than the red bar's high
 */

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class OverBoughtSold : Indicator
    {
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Over-bought level", DefaultValue = 67)]
        public int RsiOverBoughtLevel { get; set; }

        [Parameter("Over-sold level", DefaultValue = 33)]
        public int RsiOverSoldLevel { get; set; }

        [Parameter("Bars Threshold", DefaultValue = 5)]
        public int BarsThreshold { get; set; }

        [Output("Up Signal", LineColor = "Lime")]
        public IndicatorDataSeries UpSignal { get; set; }

        [Output("Down Signal", LineColor = "White")]
        public IndicatorDataSeries DownSignal { get; set; }

        private RelativeStrengthIndex _rsi;
        private double _buffer;

        protected override void Initialize()
        {
            // Initialize and create indicators
            _rsi = Indicators.RelativeStrengthIndex(SourceSeries, 14);
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
            }
            else if (IsBearishBar(index))
            {
                DrawBearishPoint(index);
            }
        }

        private bool IsBullishBar(int index)
        {
            if (index <= 14)
            {
                // RSI requires this number of bars
                return false;
            }

            var isOverSold = false;
            for (var i = index - BarsThreshold; i <= index; i++)
            {
                if (_rsi.Result[i] < RsiOverSoldLevel)
                {
                    //Print("oversold at index {0} - {1}", index, _rsi.Result.Last(i));
                    isOverSold = true;
                    break;
                }
            }

            if (!isOverSold)
            {
                return false;
            }

            return HasBullishReversalSignal(index);
        }

        private bool IsBearishBar(int index)
        {
            if (index <= 14)
            {
                // RSI requires this number of bars
                return false;
            }

            var isOverBought = false;
            for (var i = index - BarsThreshold; i <= index; i++)
            {
                if (_rsi.Result[i] > RsiOverBoughtLevel)
                {
                    isOverBought = true;
                    break;
                }
            }

            if (!isOverBought)
            {
                return false;
            }

            return HasBearishReversalSignal(index);
        }

        private bool HasBullishReversalSignal(int index)
        {
            if (MarketSeries.Close[index - 1] < MarketSeries.Open[index - 1] &&
                MarketSeries.Close[index] > MarketSeries.Open[index] &&
                MarketSeries.Close[index] > MarketSeries.High[index - 1])
            {
                return true;
            }

            return false;
        }

        private bool HasBearishReversalSignal(int index)
        {
            if (MarketSeries.Close[index - 1] > MarketSeries.Open[index - 1] &&
                MarketSeries.Close[index] < MarketSeries.Open[index] &&
                MarketSeries.Close[index] < MarketSeries.Low[index - 1])
            {
                return true;
            }

            return false;
        }

        private void DrawBullishPoint(int index)
        {
            Print("bullish signal at index {0}", index);
            UpSignal[index] = 1.0;
            var y = MarketSeries.Low[index] - _buffer;
            Chart.DrawIcon("bullsignal" + index, ChartIconType.UpArrow, index, y, Color.Lime);
        }

        private void DrawBearishPoint(int index)
        {
            Print("bearish signal at index {0}", index);
            DownSignal[index] = 1.0;
            var y = MarketSeries.High[index] + _buffer;
            Chart.DrawIcon("bearsignal" + index, ChartIconType.DownArrow, index, y, Color.White);
        }
    }
}
