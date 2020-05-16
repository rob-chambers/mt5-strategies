// Version 2020-05-12 20:50
using cAlgo.API;
using cAlgo.API.Indicators;
using Powder.TradingLibrary;
using System;
using System.Linq;

namespace cAlgo.Library.Robots.TakeOutStopsBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class TakeOutStopsBot : BaseRobot
    {
        private const string SignalGroup = "Signal";
        private const string RiskGroup = "Risk Management";
        private AverageTrueRange _atr;
        private double _initialStopPrice;

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

            if (!TakeLongsParameter && !TakeShortsParameter)
            {
                throw new ArgumentException("Both longs and shorts are disabled");
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
            var buffer = _atr.Result.LastValue / 4;
            var entryPrice = Bars.HighPrices.Last(1) + buffer;
            _initialStopPrice = Bars.LowPrices.Last(1) - buffer;

            var label = string.Format("BUY {0}", Symbol);
            double? stopLossPips = Math.Round((entryPrice - _initialStopPrice) / Symbol.PipSize, 1);

            const int BarsToExpireOrder = 10;

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * GetTimeFrameInMinutes());            

            Print("Placing BUY STOP order at {0} with stop {1}", entryPrice, stopLossPips);
            var takeProfitPips = CalculateTakeProfit(stopLossPips);
            PlaceStopOrder(TradeType.Buy, Symbol.Name, volumeInUnits, entryPrice, label, stopLossPips, takeProfitPips, expiry);

            RecentLow = InitialRecentLow;
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

        protected override bool ManageLongPosition()
        {
            // Are we making higher highs?
            var madeNewHigh = false;

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            if (Symbol.Ask > RecentHigh + buffer && _currentPosition.Pips > 0)
            {
                madeNewHigh = true;
                RecentHigh = Math.Max(Symbol.Ask, Bars.HighPrices.Maximum(BarsSinceEntry + 1));
                Print("Recent high set to {0}", RecentHigh);
            }

            if (!madeNewHigh)
            {
                return true;
            }

            var diff = _currentPosition.EntryPrice - _initialStopPrice;
            var trailPrice = _currentPosition.EntryPrice + diff / 2;

            double newStop;
            if (Symbol.Ask > trailPrice)
            {
                // Don't lose more than half our profit
                newStop = _currentPosition.EntryPrice + _currentPosition.Pips / 2 * Symbol.PipSize;

                //var stop = CalulateTrailingStopForLongPosition();

                AdjustStopLossForLongPosition(newStop);
            }

            return true;
        }

        protected override void OnBar()
        {
            if (PendingOrders.Any())
            {
                CheckToAdjustPendingOrder();
            }

            base.OnBar();
        }

        private void CheckToAdjustPendingOrder()
        {
            var pendingOrder = PendingOrders.SingleOrDefault();
            if (pendingOrder == null)
                return;

            if (pendingOrder.TradeType == TradeType.Buy)
            {
                CheckToAdjustLongPendingOrder(pendingOrder);
            }
            else
            {
                CheckToAdjustShortPendingOrder(pendingOrder);
            }
        }

        private void CheckToAdjustLongPendingOrder(PendingOrder order)
        {
            // Are we making lower lows?
            var madeNewLow = false;

            if (Symbol.Ask < RecentLow)
            {
                madeNewLow = true;
                RecentLow = Math.Min(Symbol.Ask, Bars.LowPrices.Minimum(BarsSinceEntry + 1));
                Print("Recent low set to {0}", RecentLow);
            }

            if (!madeNewLow) return;

            var newStop = CalculateStopLossInPips(order);

            Print("Moving initial stop loss on pending order lower, down to {0}", newStop);
            order.ModifyStopLossPips(newStop);
        }

        private double CalculateStopLossInPips(PendingOrder order)
        {
            var buffer = _atr.Result.LastValue / 4;
            var stop = order.TargetPrice - (RecentLow - buffer);

            return Math.Round(stop / Symbol.PipSize, 1);
        }

        private void CheckToAdjustShortPendingOrder(PendingOrder pendingOrder)
        {           
        }
    }
}