// Version 2020-05-17 15:29
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System;
using System.Linq;

namespace cAlgo.Library.Robots.TakeOutStopsBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class TakeOutStopsBot : BaseRobot
    {
        private static class GroupNames
        {
            public const string Signal = "Signal";
            public const string Risk = "Risk Management";
            public const string Notifications = "Notifications";
        }

        private AverageTrueRange _atr;
        private double _initialStopPrice;
        private Spring _spring;
        private int _timeFrameInMinutes;
        private double _minimumBuffer;

        #region Risk Parameters

        [Parameter("Lot Sizing Rule", Group = GroupNames.Risk, DefaultValue = LotSizingRuleValues.Dynamic)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", Group = GroupNames.Risk, DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 9, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = TrailingStopLossRuleValues.OppositeColourBar, Group = GroupNames.Risk)]
        public TrailingStopLossRuleValues TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 7, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int TrailingStopLossInPips { get; set; }

        #endregion

        #region Signal Parameters

        [Parameter(Group = GroupNames.Signal)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("SignalBarRangeMultiplier", DefaultValue = 1.2, MinValue = 0.5, MaxValue = 5, Step = 0.1, Group = GroupNames.Signal)]
        public double SignalBarRangeMultiplier { get; set; }

        [Parameter("MA Flat Filter", DefaultValue = false, Group = GroupNames.Signal)]
        public bool MaFlatFilter { get; set; }

        [Parameter("Breakout Filter", DefaultValue = true, Group = GroupNames.Signal)]
        public bool BreakoutFilter { get; set; }

        [Parameter("Min Bars For Lowest Low", DefaultValue = 65, MinValue = 10, MaxValue = 100, Step = 5, Group = GroupNames.Signal)]
        public int MinimumBarsForLowestLow { get; set; }

        [Parameter("Swing High Strength", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = GroupNames.Signal)]
        public int SwingHighStrength { get; set; }

        [Parameter("Big Move Filter", DefaultValue = true, Group = GroupNames.Signal)]
        public bool BigMoveFilter { get; set; }

        [Parameter("Bars To Expire Order", DefaultValue = 12, MinValue = 3, MaxValue =20, Group = GroupNames.Signal)]
        public int BarsToExpireOrder { get; set; }

        #endregion

        #region Notification Parameters

        [Parameter("Send email alerts", DefaultValue = false, Group = GroupNames.Notifications)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = GroupNames.Notifications)]
        public bool PlayAlertSound { get; set; }

        #endregion

        #region Trade Management Parameters

        [Parameter("Bars for trade development", DefaultValue = 3)]
        public int BarsToAllowTradeToDevelop { get; set; }

        #endregion

        protected override string Name
        {
            get
            {
                return "TakeOutStopsBot";
            }
        }

        protected override void OnStart()
        {
            Print("Lot sizing rule: {0}", LotSizingRule);

            var symbolLeverage = Symbol.DynamicLeverage[0].Leverage;
            Print("Symbol leverage: {0}", symbolLeverage);

            var realLeverage = Math.Min(symbolLeverage, Account.PreciseLeverage);
            Print("Account leverage: {0}", Account.PreciseLeverage);

            Init(true,
                false,
                InitialStopLossRuleValues.None,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
                TakeProfitRuleValues.None,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                BarsToAllowTradeToDevelop);

            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            _spring = Indicators.GetIndicator<Spring>(SourceSeries, 89, 55, 21, SendEmailAlerts, PlayAlertSound, 
                SignalBarRangeMultiplier, MaFlatFilter, BreakoutFilter, MinimumBarsForLowestLow, SwingHighStrength, BigMoveFilter);
            _minimumBuffer = Symbol.PipSize * 6;
            _timeFrameInMinutes = GetTimeFrameInMinutes();
        }

        protected override bool HasBullishSignal()
        {
            var hasSignal = !double.IsNaN(_spring.UpSignal.Last(1));
            return hasSignal;
        }

        protected override bool HasBearishSignal()
        {
            return false;
        }

        protected override void EnterLongPosition()
        {
            var entryPrice = Bars.HighPrices.Last(1) + Symbol.PipSize * 2;
            _initialStopPrice = CalculateInitialStopLossInPipsForLongPosition().Value;
            
            double? stopLossPips = Math.Round((entryPrice - _initialStopPrice) / Symbol.PipSize, 1);
            var lots = CalculatePositionQuantityInLots(stopLossPips.Value);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * _timeFrameInMinutes);            

            Print("Placing BUY STOP order at {0} with stop {1}", entryPrice, stopLossPips);
            var takeProfitPips = CalculateTakeProfit(stopLossPips);
            var label = string.Format("BUY {0}", Symbol);
            PlaceStopOrder(TradeType.Buy, Symbol.Name, volumeInUnits, entryPrice, label, stopLossPips, takeProfitPips, expiry);

            // Reset recent low
            RecentLow = InitialRecentLow;
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            // Assume we're on the signal bar
            var low = Bars.LowPrices.Last(1);
            return low - GetBuffer();
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

            throw new ArgumentOutOfRangeException("Unsupported timeframe");
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {            
            base.OnPositionOpened(args);
            ShouldTrail = false;
        }

        protected override bool ManageLongPosition()
        {
            if (BarsSinceEntry <= BarsToAllowTradeToDevelop)
                return false;

            if (!ShouldTrail && Symbol.Ask > TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop higher
            return base.ManageLongPosition();
        }

        protected override void OnBar()
        {
            if (PendingOrders.Any())
                CheckToAdjustPendingOrder();

            base.OnBar();
        }

        private void CheckToAdjustPendingOrder()
        {
            var pendingOrder = PendingOrders.SingleOrDefault();
            if (pendingOrder == null)
                return;

            if (pendingOrder.TradeType == TradeType.Buy)
                CheckToAdjustLongPendingOrder(pendingOrder);
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

            if (!madeNewLow) 
                return;

            var newStop = CalculateStopLossInPips(order);

            // Safety check - is the new stop actually lower than the current stop?  i.e. Is the stop BIGGER in pips?
            if (order.StopLossPips.HasValue && newStop > order.StopLossPips.Value)
            {
                Print("Moving initial stop loss on pending order lower - {0} pips from entry price", newStop);
                order.ModifyStopLossPips(newStop);
            }
        }

        private double CalculateStopLossInPips(PendingOrder order)
        {
            var buffer = GetBuffer();
            var stop = order.TargetPrice - RecentLow - buffer;

            return Math.Round(stop / Symbol.PipSize, 1);
        }

        private double GetBuffer()
        {
            var buffer = _atr.Result.LastValue / 4;           
            if (buffer < _minimumBuffer)
                buffer = _minimumBuffer;

            return buffer;
        }
    }
}