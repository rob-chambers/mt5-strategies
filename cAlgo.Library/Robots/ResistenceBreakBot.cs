// Version 2020-12-27 15:44
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System;
using System.Linq;

namespace cAlgo.Library.Robots.TakeOutStopsBot
{
    public enum MaCrossRule
    {
        None,
        CloseOnFastMaCross,
        CloseOnMediumMaCross,
        CloseOnSlowMaCross
    }

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
        //private int _timeFrameInMinutes;
        private double _minimumBuffer;
        private double _entryPrice, _stopLossPips;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;

        #region Risk Parameters

        [Parameter("Lot Sizing Rule", Group = GroupNames.Risk, DefaultValue = LotSizingRuleValues.Dynamic)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", Group = GroupNames.Risk, DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = InitialStopLossRuleValues.PreviousBarNPips)]
        public InitialStopLossRuleValues InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = TrailingStopLossRuleValues.None, Group = GroupNames.Risk)]
        public TrailingStopLossRuleValues TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 7, MinValue = 0, MaxValue = 20, Group = GroupNames.Risk)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Take Profit Rule", DefaultValue = TakeProfitRuleValues.None, Group = GroupNames.Risk)]
        public TakeProfitRuleValues TakeProfitRule { get; set; }

        #endregion

        #region Signal Parameters

        [Parameter(Group = GroupNames.Signal)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Swing High Strength", DefaultValue = 5, MinValue = 1, MaxValue = 5, Group = GroupNames.Signal)]
        public int SwingHighStrength { get; set; }

        [Parameter("Bars To Expire Order", DefaultValue = 4 * 12, MinValue = 3, MaxValue = 24 * 4, Group = GroupNames.Signal)]
        public int BarsToExpireOrder { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = MaCrossRule.CloseOnFastMaCross)]
        public MaCrossRule MaCrossRule { get; set; }

        [Parameter("MAs Stacked Filter", DefaultValue = false, Group = GroupNames.Signal)]
        public bool MovingAveragesStackedFilter { get; set; }

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
                InitialStopLossRule,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
                TakeProfitRule,
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
            //_timeFrameInMinutes = GetTimeFrameInMinutes();
            PendingOrders.Cancelled += OnPendingOrdersCancelled;

            _fastMA = Indicators.MovingAverage(SourceSeries, 21, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, 55, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, 89, MovingAverageType.Simple);
        }

        private void OnPendingOrdersCancelled(PendingOrderCancelledEventArgs args)
        {
            Print("Pending order cancelled: {0}", args.Reason);
        }

        protected override bool HasBullishSignal()
        {
            var hasSignal = !double.IsNaN(_resistenceBreak.UpSignal.Last(1));
            return hasSignal && (!MovingAveragesStackedFilter || AreMovingAveragesStackedBullishly());
        }

        private bool AreMovingAveragesStackedBullishly()
        {
            return _fastMA.Result.LastValue > _mediumMA.Result.LastValue &&
                _mediumMA.Result.LastValue > _slowMA.Result.LastValue;
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
            SubmitMarketOrder();
        }
        
        private void SubmitMarketOrder()
        {
            //var entryPrice = _entryPrice;
            var stopLoss = _stopLossPips;
            Print("SL: {0}", stopLoss);

            var lots = CalculatePositionQuantityInLots(stopLoss);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);

            //var expiry = Server.Time.AddMinutes(BarsToExpireOrder * _timeFrameInMinutes);

            Print("Executing market order with a {0} pip stop", stopLoss);
            var takeProfitPips = CalculateTakeProfit(stopLoss);
            var label = string.Format("BUY {0}", Symbol);

            ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volumeInUnits, label, stopLoss, takeProfitPips);

            // Reset recent low
            RecentLow = InitialRecentLow;
        }

        //protected override double? CalculateInitialStopLossInPipsForLongPosition()
        //{
        //    Print("Calculating initial SL");
        //    Print("Last value: {0}, last 1: {1}", _atr.Result.LastValue, _atr.Result.Last(1));

        //    var stop = _atr.Result.LastValue / Symbol.PipSize * 2;
        //    Print("Stop in pips: {0}", stop);

        //    // Min 6 pip stop.  The stop should be the bigger value between the min and the calculated stop.
        //    stop = Math.Max(stop, 6);
        //    return Math.Round(stop, 1);
        //}

        //private int GetTimeFrameInMinutes()
        //{
        //    if (Bars.TimeFrame == TimeFrame.Minute)
        //        return 1;
        //    else if (Bars.TimeFrame == TimeFrame.Minute5)
        //        return 5;
        //    else if (Bars.TimeFrame == TimeFrame.Minute15)
        //        return 15;
        //    else if (Bars.TimeFrame == TimeFrame.Hour)
        //        return 60;

        //    throw new ArgumentOutOfRangeException("Unsupported timeframe");
        //}

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
            if (!base.ManageLongPosition()) return false;

            double value;
            string maType;

            switch (MaCrossRule)
            {
                case MaCrossRule.CloseOnSlowMaCross:
                    value = _slowMA.Result.LastValue;
                    maType = "slow";
                    break;

                case MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                case MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                default:
                    return true;
            }

            if (Bars.ClosePrices.Last(1) < value - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the {0} MA", maType);
                _currentPosition.Close();
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