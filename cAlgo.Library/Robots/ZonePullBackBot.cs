// Version 2020-12-31 17:28
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace cAlgo.Library.Robots.ZonePullBackBot
{
    public enum MaCrossRule
    {
        None,
        CloseOnFastMaCross,
        CloseOnMediumMaCross,
        CloseOnSlowMaCross
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class ZonePullBackBot : BaseRobot
    {
        private const string LogFilePath = @"Z:\Documents\Trading\cTrader";

        private static class GroupNames
        {
            public const string Signal = "Signal";
            public const string Risk = "Risk Management";
            public const string Notifications = "Notifications";
        }

        private ZonePullBack _zonePullBack;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private int _timeFrameInMinutes;
        private string _logFileName;

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

        [Parameter("Bars for trade development", DefaultValue = 20, Group = GroupNames.Risk)]
        public int BarsToAllowTradeToDevelop { get; set; }

        #endregion

        #region Signal Parameters

        [Parameter(Group = GroupNames.Signal)]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Bars To Expire Order", DefaultValue = 10, MinValue = 3, MaxValue = 16, Group = GroupNames.Signal)]
        public int BarsToExpireOrder { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = MaCrossRule.CloseOnFastMaCross)]
        public MaCrossRule MaCrossRule { get; set; }

        [Parameter("MAs Range Filter", DefaultValue = false, Group = GroupNames.Signal)]
        public bool MaRangeFilter { get; set; }

        [Parameter("Stacked MAs Filter", DefaultValue = false, Group = GroupNames.Signal)]
        public bool StackedMasFilter { get; set; }

        [Parameter("Long term trend Filter", DefaultValue = false, Group = GroupNames.Signal)]
        public bool LongTermTrendFilter { get; set; }

        [Parameter("Adjust pending order", DefaultValue = true, Group = GroupNames.Signal)]
        public bool AdjustPendingOrder { get; set; }

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
                return "ZonePullBackBot";
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
                BarsToAllowTradeToDevelop);

            _zonePullBack = Indicators.GetIndicator<ZonePullBack>(
                SourceSeries, SendEmailAlerts, PlayAlertSound, ShowMessage, 
                MaRangeFilter, StackedMasFilter, LongTermTrendFilter);
            
            PendingOrders.Cancelled += OnPendingOrdersCancelled;

            _fastMA = Indicators.MovingAverage(SourceSeries, 21, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, 55, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, 89, MovingAverageType.Simple);

            _timeFrameInMinutes = GetTimeFrameInMinutes();

            var fileName = string.Format("{0}_M{1}.csv", Symbol.Name, _timeFrameInMinutes);
            _logFileName = Path.Combine(LogFilePath, fileName);
            WriteLogFileHeader();
        }

        protected override void OnBar()
        {
            if (AdjustPendingOrder && PendingOrders.Any())
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
            var low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(0));
            var stopLossPips = Math.Round(InitialStopLossInPips + (order.TargetPrice - low) / Symbol.PipSize, 1);

            if (stopLossPips > order.StopLossPips)
            {
                AdjustBuyStopOrder(order, stopLossPips);
            }
        }

        private void AdjustBuyStopOrder(PendingOrder order, double stopLoss)
        {
            var lots = CalculatePositionQuantityInLots(stopLoss);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * _timeFrameInMinutes);

            Print("Adjusting Buy Stop order to {0} pip stop loss and {1} volume",
                stopLoss, volumeInUnits);
            var takeProfitPips = CalculateTakeProfit(stopLoss);
            var label = string.Format("BUY STOP {0}", Symbol);

            ModifyPendingOrder(order, order.TargetPrice, stopLoss, takeProfitPips, expiry, volumeInUnits);

            // Reset recent low
            RecentLow = InitialRecentLow;
        }

        private void WriteLogFileHeader()
        {
            var header = "Entry Time,Entry,Stop,Volume,O,H,L,C,21EMA,55EMA,89MA";
            header += Environment.NewLine;
            File.WriteAllText(_logFileName, header);
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

        private void OnPendingOrdersCancelled(PendingOrderCancelledEventArgs args)
        {
            Print("Pending order cancelled: {0}", args.Reason);
        }

        protected override bool HasBullishSignal()
        {
            var hasSignal = !double.IsNaN(_zonePullBack.UpSignal.Last(1));
            return hasSignal;
        }

        protected override bool HasBearishSignal()
        {
            return false;
        }

        protected override void EnterLongPosition()
        {
            var buffer = 2 * Symbol.PipSize;

            var entryPrice = Bars.HighPrices.Last(1) + buffer;
            //var stopLossPips = CalculateInitialStopLossInPipsForLongPosition().Value;

            var low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(0));
            var stopLossPips = Math.Round(InitialStopLossInPips + (entryPrice - low) / Symbol.PipSize, 1);
            SubmitBuyStopOrder(entryPrice, stopLossPips);
        }

        private void SubmitBuyStopOrder(double entryPrice, double stopLoss)
        {
            var lots = CalculatePositionQuantityInLots(stopLoss);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);

            var expiry = Server.Time.AddMinutes(BarsToExpireOrder * _timeFrameInMinutes);

            Print("Placing Buy Stop order with a {0} pip stop loss", stopLoss);
            var takeProfitPips = CalculateTakeProfit(stopLoss);
            var label = string.Format("BUY STOP {0}", Symbol);

            PlaceStopOrder(TradeType.Buy, Symbol.Name, volumeInUnits, entryPrice, label, stopLoss, takeProfitPips, expiry);

            // Reset recent low
            RecentLow = InitialRecentLow;
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {
            base.OnPositionOpened(args);
            ShouldTrail = false;

            //PrintLatestValues();

            LogSignalData(args);
        }

        private void PrintLatestValues()
        {
            Print("Current date: {0}", Server.Time.ToString("dd/MMM/yyyy HH:mm"));
            Print("Current date (UTC): {0}", Server.Time.ToUniversalTime());

            Print("Symbol Ask: {0}", Symbol.Ask);
            Print("Symbol Bid: {0}", Symbol.Bid);

            Print("Latest close: {0}", Bars.ClosePrices.LastValue);
            Print("Close index 0: {0}", Bars.ClosePrices.Last(0));
            Print("Close index 1: {0}", Bars.ClosePrices.Last(1));

            Print("Latest high: {0}", Bars.HighPrices.LastValue);
            Print("High index 0: {0}", Bars.HighPrices.Last(0));
            Print("High index 1: {0}", Bars.HighPrices.Last(1));

            Print("Moving Average Data");
            Print("Fast MA Latest: {0}", _fastMA.Result.LastValue);
            Print("Fast MA Index 0: {0}", _fastMA.Result.Last(0));
            Print("Fast MA Index 1: {0}", _fastMA.Result.Last(1));
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

        private void LogSignalData(PositionOpenedEventArgs args)
        {
            var position = args.Position;

            var contents = new StringBuilder();
            WriteColumn(contents, position.EntryTime.ToLocalTime().ToString("dd/MMM/yyyy HH:mm"));
            WriteColumn(contents, position.EntryPrice);
            WriteColumn(contents, position.StopLoss.GetValueOrDefault());
            WriteColumn(contents, position.VolumeInUnits);

            WriteColumn(contents, Bars.OpenPrices.Last(1));
            WriteColumn(contents, Bars.HighPrices.Last(1));
            WriteColumn(contents, Bars.LowPrices.Last(1));
            WriteColumn(contents, Bars.ClosePrices.Last(1));
            WriteColumn(contents, _fastMA.Result.Last(1));
            WriteColumn(contents, _mediumMA.Result.Last(1));
            
            contents.Append(_slowMA.Result.Last(1));

            contents.AppendLine();

            File.AppendAllText(_logFileName, contents.ToString());
        }

        private static void WriteColumn(StringBuilder contents, object value)
        {
            contents.Append(value);
            contents.Append(",");
        }
    }
}