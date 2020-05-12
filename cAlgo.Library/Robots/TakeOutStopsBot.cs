// Version 2020-05-12 17:08
using cAlgo.API;
using cAlgo.API.Indicators;
using Powder.TradingLibrary;
using System;

namespace cAlgo.Library.Robots.TakeOutStopsBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class TakeOutStopsBot : BaseRobot
    {
        private const string SignalGroup = "Signal";
        private const string RiskGroup = "Risk Management";
        private AverageTrueRange _atr;

        [Parameter("Take long trades?", Group = SignalGroup, DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", Group = "Signal", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter(Group = SignalGroup)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Lot Sizing Rule", Group = RiskGroup, DefaultValue = LotSizingRuleValues.Static)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", Group = RiskGroup, DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        protected override string Name
        {
            get
            {
                return "TakeOutStopsBot";
            }
        }

        protected override void OnStart()
        {
            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);

            if (TakeLongsParameter && TakeShortsParameter)
            {
                throw new ArgumentException("This Robot is designed to either go long or short but not both at the same time");
            }
            else if (!TakeLongsParameter && !TakeShortsParameter)
            {
                throw new ArgumentException("You need to decide whether to go long or short");
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
                TakeProfitRuleValues.DoubleRisk,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                0);

            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
        }

        protected override bool HasBullishSignal()
        {
            // 1) Look for a larger bar than normal.  Range should be e.g. 1.5 x average
            var priorBarRange = Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1);
            const double RangeMultiplier = 1.5;

            if (priorBarRange < _atr.Result.Last(1) * RangeMultiplier)
            {
                return false;
            }

            // 2) We must have made a new low over x bars
            const int BarsToTestForNewLowHigh = 40;

            if (Common.IndexOfLowestLow(Bars.ClosePrices, BarsToTestForNewLowHigh) != 1)
            {
                return false;
            }

            return true;
        }

        protected override bool HasBearishSignal()
        {
            return false;
        }

        protected override void EnterLongPosition()
        {
            var volumeInUnits = 100000;

            //var priorBarRange = Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1);
            //priorBarRange /= 4;
            var buffer = _atr.Result.LastValue / 5;
            var entryPrice = Bars.HighPrices.Last(1) + buffer;
            var stopPrice = Bars.LowPrices.Last(1) - buffer;

            var label = string.Format("BUY {0}", Symbol);
            double? stopLossPips = Math.Round((entryPrice - stopPrice) / Symbol.PipSize, 1);

            const int BarsToExpireOrder = 10;

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * GetTimeFrameInMinutes());            

            Print("Placing BUY STOP order at {0} with stop {1}", entryPrice, stopLossPips);
            var takeProfitPips = CalculateTakeProfit(stopLossPips);
            PlaceStopOrder(TradeType.Buy, Symbol.Name, volumeInUnits, entryPrice, label, stopLossPips, takeProfitPips, expiry);
        }
        
        private int GetTimeFrameInMinutes()
        {
            if (Bars.TimeFrame == TimeFrame.Minute)
                return 1;
            else if (Bars.TimeFrame == TimeFrame.Minute5)
                return 5;
            else if (Bars.TimeFrame == TimeFrame.Minute15)
                return 15;
            else if (Bars.TimeFrame == TimeFrame.Hour)
                return 60;

            throw new ArgumentOutOfRangeException("Invalid timeframe");
        }
    }
}