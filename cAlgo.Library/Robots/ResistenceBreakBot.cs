// Version 2020-12-20 17:18
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System;
using System.Linq;

namespace cAlgo.Library.Robots.TakeOutStopsBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class ResistenceBreakBot : BaseRobot
    {
        private static class GroupNames
        {
            public const string Signal = "Signal";
            public const string Risk = "Risk Management";
            public const string Notifications = "Notifications";
        }

        private AverageTrueRange _atr;
        private ResistenceBreak _resistenceBreak;
        private int _timeFrameInMinutes;
        private double _minimumBuffer;
        private int _barCountSinceSignal;
        private double _entryPrice, _stopLossPips;

        #region Risk Parameters

        [Parameter("Lot Sizing Rule", Group = GroupNames.Risk, DefaultValue = LotSizingRuleValues.Static)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", Group = GroupNames.Risk, DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 9, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = TrailingStopLossRuleValues.None, Group = GroupNames.Risk)]
        public TrailingStopLossRuleValues TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 7, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int TrailingStopLossInPips { get; set; }

        #endregion

        #region Signal Parameters

        [Parameter(Group = GroupNames.Signal)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Swing High Strength", DefaultValue = 5, MinValue = 1, MaxValue = 5, Group = GroupNames.Signal)]
        public int SwingHighStrength { get; set; }

        [Parameter("Bars To Expire Order", DefaultValue = 4 * 12, MinValue = 3, MaxValue = 24 * 4, Group = GroupNames.Signal)]
        public int BarsToExpireOrder { get; set; }

        #endregion

        #region Notification Parameters

        [Parameter("Send email alerts", DefaultValue = false, Group = GroupNames.Notifications)]
        public bool SendEmailAlerts { get; set; }

        [Parameter("Play alert sound", DefaultValue = false, Group = GroupNames.Notifications)]
        public bool PlayAlertSound { get; set; }

        [Parameter("Show alert message", DefaultValue = false, Group = GroupNames.Notifications)]
        public bool ShowMessage { get; set; }

        #endregion

        protected override string Name
        {
            get
            {
                return "ResistenceBreakBot";
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
                TakeProfitRuleValues.DoubleRisk,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                0);

            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            _resistenceBreak = Indicators.GetIndicator<ResistenceBreak>(SourceSeries, SendEmailAlerts, PlayAlertSound, ShowMessage,
                SwingHighStrength);
            _minimumBuffer = Symbol.PipSize * 6;
            _timeFrameInMinutes = GetTimeFrameInMinutes();
            _barCountSinceSignal = -1;

            PendingOrders.Cancelled += OnPendingOrdersCancelled;
        }

        private void OnPendingOrdersCancelled(PendingOrderCancelledEventArgs args)
        {
            Print("Pending order cancelled: {0}", args.Reason);
            _barCountSinceSignal = -1;
        }

        protected override bool HasBullishSignal()
        {
            /* This method gets called on every bar so we can use that fact
             * to submit a pending order after x bars after we receive a signal
             */ 

            if (_barCountSinceSignal == -1)
            {
                var hasSignal = !double.IsNaN(_resistenceBreak.UpSignal.Last(1));
                return hasSignal;
            }

            return false;
        }

        protected override bool HasBearishSignal()
        {
            return false;
        }

        protected override void EnterLongPosition()
        {
            // Don't enter here - instead wait until x bars have passed
            // before placing a pending order
            _entryPrice = _resistenceBreak.UpSignal.Last(1);
            _stopLossPips = CalculateInitialStopLossInPipsForLongPosition().Value;

            // Ensure we no longer search for a signal
            _barCountSinceSignal = 0;
        }
        
        private void SubmitPendingOrder()
        {
            var entryPrice = _entryPrice;
            var stopLoss = _stopLossPips;
            Print("SL: {0}", stopLoss);

            var lots = CalculatePositionQuantityInLots(stopLoss);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * _timeFrameInMinutes);

            Print("Placing BUY LIMIT order at {0} with a {1} pip stop", entryPrice, stopLoss);
            var takeProfitPips = CalculateTakeProfit(stopLoss);
            var label = string.Format("BUY {0}", Symbol);

            PlaceLimitOrder(TradeType.Buy, Symbol.Name, volumeInUnits, entryPrice, label, stopLoss, takeProfitPips, expiry);

            // Reset recent low
            RecentLow = InitialRecentLow;
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            Print("Calculating initial SL");
            Print("Last value: {0}, last 1: {1}", _atr.Result.LastValue, _atr.Result.Last(1));

            var stop = _atr.Result.LastValue / Symbol.PipSize;
            Print("Stop in pips: {0}", stop);

            // Min 4 pip stop.  The stop should be the bigger value between the min and the calculated stop.
            stop = Math.Max(stop, 4);
            return Math.Round(stop, 1);
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
            {
                CheckToAdjustPendingOrder();
            }
            else if (!Positions.Any() && _barCountSinceSignal >= 0)
            {
                // We've had a signal - check if it's time to submit the order
                _barCountSinceSignal++;
                if (_barCountSinceSignal >= 10)
                {
                    if (ValidConditionsForOrder())
                    {
                        SubmitPendingOrder();
                    }
                    else
                    {
                        // A poor signal - Start looking for a new signal
                        _barCountSinceSignal = -1;
                    }
                }
            }

            base.OnBar();
        }

        private bool ValidConditionsForOrder()
        {
            // Check the lows over the last x bars.  They should be above the entry price
            var low = Bars.LowPrices.Minimum(5);

            return low > _entryPrice;
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            _barCountSinceSignal = -1;
            base.OnPositionClosed(args);
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
            return;
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