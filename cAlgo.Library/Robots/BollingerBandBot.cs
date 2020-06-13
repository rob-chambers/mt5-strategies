// Version 2020-06-10 17:34
using cAlgo.API;
using cAlgo.API.Indicators;
using Powder.TradingLibrary;
using System;

namespace cAlgo.Library.Robots.QmpFilterBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class BollingerBandBot : BaseRobot
    {
        private const string SignalGroup = "Signal";
        private const string RiskGroup = "Risk Management";

        [Parameter("Take long trades?", Group = SignalGroup, DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", Group = "Signal", DefaultValue = true)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Source", Group = SignalGroup)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", Group = SignalGroup, DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Fast MA Period", Group = SignalGroup, DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Lot Sizing Rule", Group = RiskGroup, DefaultValue = LotSizingRuleValues.Static)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", Group = RiskGroup, DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        protected override string Name
        {
            get
            {
                return "BollingerBandBot";
            }
        }

        private BollingerBands _bollingerBands;
        private MovingAverage _slowMA;
        private MovingAverage _fastMA;

        protected override void OnStart()
        {
            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);

            if (!TakeLongsParameter && !TakeShortsParameter)
            {
                throw new ArgumentException("You need to decide whether to go long/short or both");
            }

            Print("Lot sizing rule: {0}", LotSizingRule);

            var symbolLeverage = Symbol.DynamicLeverage[0].Leverage;
            Print("Symbol leverage: {0}", symbolLeverage);

            var realLeverage = Math.Min(symbolLeverage, Account.PreciseLeverage);
            Print("Account leverage: {0}", Account.PreciseLeverage);

            Init(TakeLongsParameter,
                TakeShortsParameter,
                InitialStopLossRuleValues.None,
                0,
                TrailingStopLossRuleValues.None,
                0,
                LotSizingRule,
                TakeProfitRuleValues.None,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                6);

            _bollingerBands = Indicators.BollingerBands(SourceSeries, 20, 1, MovingAverageType.Simple);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
        }

        protected override bool HasBullishSignal()
        {
            var close = SourceSeries.Last(1);

            // Price must be > slow EMA
            if (close <= _slowMA.Result.Last(1))
                return false;

            // The close must have crossed above the upper Bollinger band 
            if (!SourceSeries.HasCrossedAbove(_bollingerBands.Top, 1))
                return false;

            // Ensure price has been between the 2 bands
            for (var index = 2; index < 10; index++)
            {
                var price = SourceSeries.Last(index);
                if (price > _bollingerBands.Top.Last(index) || price < _bollingerBands.Bottom.Last(index))
                {
                    return false;
                }
            }

            // Measure Band volatility - we want it to have contracted
            if (!HaveBollingerBandsContracted())
            {
                return false;
            }
            
            // The close must be > open and the close must be near the high and the open near the low
            if (!ApplyCandleFilter(true))
            {
                return false;
            }

            ApplyWeighting(true);

            return true;
        }

        private bool HaveBollingerBandsContracted()
        {
            const int CurrentIndex = 1;
            const int RecentIndex = 12;
            const int PriorIndex = 40;

            var diff = _bollingerBands.Top.Last(CurrentIndex) - _bollingerBands.Bottom.Last(CurrentIndex);
            var diff2 = _bollingerBands.Top.Last(RecentIndex) - _bollingerBands.Bottom.Last(RecentIndex);
            var diff3 = _bollingerBands.Top.Last(PriorIndex) - _bollingerBands.Bottom.Last(PriorIndex);

            return diff3 / diff >= 1.8 &&
                diff2 / diff > 1.2 && 
                diff3 > diff2;
        }

        protected override bool HasBearishSignal()
        {
            var close = SourceSeries.Last(1);

            // Price must be < slow EMA
            if (close >= _slowMA.Result.Last(1))
                return false;

            // The close must have crossed below the lower Bollinger band 
            if (!SourceSeries.HasCrossedBelow(_bollingerBands.Bottom, 1))
                return false;

            // Have we had any other crosses recently?
            for (var index = 2; index < 10; index++)
            {
                var price = SourceSeries.Last(index);
                if (price > _bollingerBands.Top.Last(index) || price < _bollingerBands.Bottom.Last(index))
                {
                    return false;
                }
            }

            // Measure Band volatility - we want it to have contracted
            if (!HaveBollingerBandsContracted())
            {
                return false;
            }

            if (!ApplyCandleFilter(false))
            {
                return false;
            }

            ApplyWeighting(true);

            return true;
        }

        private bool ApplyCandleFilter(bool isLong)
        {
            var currentLow = Bars.LowPrices.Last(1);
            var currentHigh = Bars.HighPrices.Last(1);
            var currentOpen = Bars.OpenPrices.Last(1);
            var closeFromHigh = currentHigh - SourceSeries.Last(1);
            var openFromLow = currentOpen - currentLow;
            var range = currentHigh - currentLow;

            if (isLong)
            {
                // The close must be > open and the close must be near the high and the open near the low
                return (closeFromHigh / range <= 0.2) && (openFromLow / range <= 0.2);
            }
            else
            {
                // The close must be < open and the close must be near the low and the open near the high
                return (closeFromHigh / range >= 0.8) && (openFromLow / range >= 0.8);
            }
        }

        private void ApplyWeighting(bool isLong)
        {
            var low = Bars.LowPrices.Last(1);
            var high = Bars.HighPrices.Last(1);

            var highLowCross = high > _fastMA.Result.Last(1) &&
                low < _fastMA.Result.Last(1) &&
                high > _slowMA.Result.Last(1) &&
                low < _slowMA.Result.Last(1);

            if (!highLowCross)
                return;

            if (isLong)
            {
                if (Bars.ClosePrices.Last(1) > _fastMA.Result.Last(1))
                {
                    // Strong cross above both MAs
                    EntryWeighting = Weighting.VeryStrong;
                }
            }
            else
            {
                if (Bars.ClosePrices.Last(1) < _fastMA.Result.Last(1))
                {
                    // Strong cross below both MAs
                    EntryWeighting = Weighting.VeryStrong;
                }
            }
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            // Assume we're on the signal bar           
            var band = _bollingerBands.Bottom.Last(1);
            Print("SL calculated at " + band);

            var pips = (Bars.ClosePrices.Last(1) - band) / Symbol.PipSize;

            return Math.Round(pips, 1);
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            // Assume we're on the signal bar
            var band = _bollingerBands.Top.Last(1);
            Print("SL calculated at " + band);

            var pips = (band - Bars.ClosePrices.Last(1)) / Symbol.PipSize;

            return Math.Round(pips, 1);
        }

        protected override bool ManageLongPosition()
        {
            // Important - call base functionality to check "bars to develop" functionality
            if (!base.ManageLongPosition()) return false;

            var value = _bollingerBands.Bottom.LastValue;
            if (Bars.ClosePrices.Last(1) < value)
            {
                Print("Closing position now that we closed below the main bollinger band of " + value);
                _currentPosition.Close();
            }

            return true;
        }

        protected override bool ManageShortPosition()
        {
            // Important - call base functionality to check "bars to develop" functionality
            if (!base.ManageShortPosition()) return false;

            var value = _bollingerBands.Top.LastValue;
            if (Bars.ClosePrices.Last(1) > value)
            {
                Print("Closing position now that we closed above the main bollinger band of " + value);
                _currentPosition.Close();
            }

            return true;
        }
    }
}
 