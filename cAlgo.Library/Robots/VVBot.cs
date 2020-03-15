using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using Powder.TradingLibrary;

// ReSharper disable InconsistentNaming
// ReSharper disable UseStringInterpolation

namespace cAlgo.Library.Robots.VVBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VVBot : BaseRobot
    {
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter] public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = 3)]
        public MaCrossRuleValues MaCrossRule { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = LotSizingRuleValues.Dynamic)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private bool _buySetup;

        protected override void OnStart()
        {
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);

            Init(TakeLongsParameter, TakeShortsParameter, MaCrossRule, LotSizingRule, DynamicRiskPercentage);
        }

        protected override bool HasBullishSignal()
        {
            var currentClose = Bars.ClosePrices.Last(1);
            var fastMA = _fastMA.Result.LastValue;
            var mediumMA = _mediumMA.Result.LastValue;
            var slowMA = _slowMA.Result.LastValue;

            // MAs must be stacked
            if (fastMA > mediumMA
                && mediumMA > slowMA
                && fastMA > slowMA)
            {
                if (_buySetup)
                {
                    // Look for a small pull-back after we get our setup
                    var trigger = currentClose < fastMA && currentClose > slowMA;
                    if (trigger)
                    {
                        // Once we trigger we don't need to maintain the setup
                        _buySetup = false;
                    }

                    return trigger;
                }

                if (currentClose > slowMA
                    && currentClose > mediumMA
                    && currentClose > fastMA)
                {
                    _buySetup = true;
                }
            }

            return false;
        }

        protected override bool HasBearishSignal()
        {
            var currentClose = Bars.ClosePrices.Last(1);

            if (currentClose < _slowMA.Result.LastValue
                && currentClose < _mediumMA.Result.LastValue
                && currentClose < _fastMA.Result.LastValue)
            {
                //return true;
                return false;
            }

            return false;
        }

        protected override double? CalculateStopLossLevelForBuyOrder()
        {
            var slowMA = _slowMA.Result.Last(1);

            var range = Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1);
            range /= 4;

            Print("Last high = {0}, Last Low = {1}, Range / 4 = {2}", Bars.HighPrices.Last(1), Bars.LowPrices.Last(1), range);

            return slowMA - range;
        }

        protected override void ManageLongPosition()
        {
            double value;
            string maType;

            switch (MaCrossRule)
            {
                case MaCrossRuleValues.CloseOnSlowMaCross:
                    value = _slowMA.Result.LastValue;
                    maType = "slow";
                    break;

                case MaCrossRuleValues.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                case MaCrossRuleValues.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                default:
                    return;
            }

            var exit = value - 2 * Symbol.PipSize;
            if (Bars.ClosePrices.Last(1) < exit)
            {
                Print("Closing position now that the price closed below {0} (i.e. the {1} MA)", exit, maType);
                CurrentPosition.Close();
            }
        }
    }

    public abstract class BaseRobot : Robot
    {
        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private bool _canOpenPosition;
        private MaCrossRuleValues _maCrossRule;
        private LotSizingRuleValues _lotSizingRule;
        private double _dynamicRiskPercentage;

        protected Position CurrentPosition { get; private set; }

        protected abstract bool HasBullishSignal();
        protected abstract bool HasBearishSignal();

        protected void Init(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            MaCrossRuleValues maCrossRule,
            LotSizingRuleValues lotSizingRule,
            double dynamicRiskPercentage)
        {
            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _maCrossRule = maCrossRule;
            _lotSizingRule = lotSizingRule;
            _dynamicRiskPercentage = dynamicRiskPercentage;
            var lotSizing = (LotSizingRuleValues)lotSizingRule;
            if (lotSizing == LotSizingRuleValues.Dynamic && (dynamicRiskPercentage <= 0 || dynamicRiskPercentage >= 10))
                throw new ArgumentOutOfRangeException($"Dynamic Risk value is out of range - it is a percentage (e.g. 2)");

            _canOpenPosition = true;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}",
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
        }

        protected abstract double? CalculateStopLossLevelForBuyOrder();

        protected override void OnBar()
        {
            if (!_canOpenPosition)
            {
                return;
            }

            if (PendingOrders.Count > 0)
            {
                return;
            }

            double quantity = 1;

            if (_takeLongsParameter && HasBullishSignal()) 
            {
                var stopLossLevel = CalculateStopLossLevelForBuyOrder();
                if (stopLossLevel.HasValue)
                {
                    quantity = CalculatePositionQuantityInLots(Symbol.Ask - stopLossLevel.Value);
                }
                
                var volumeInUnits = Symbol.QuantityToVolumeInUnits(quantity);

                Print("Symbol pip size: {0}", Symbol.PipSize);
                Print("Symbol pip value: {0}", Symbol.PipValue);
                Print("Symbol lot size: {0}", Symbol.LotSize);

                Print("Buying {0} units (quantity of {1}) at market with SL of {2}", volumeInUnits, quantity, stopLossLevel);
                var label = string.Format("Buy @ {0}", Symbol.Ask);
                ExecuteMarketRangeOrder(TradeType.Buy, Symbol.Name, volumeInUnits, 5, Symbol.Ask, label, Symbol.Ask - stopLossLevel.GetValueOrDefault(), null);
            }
            //else if (_takeShortsParameter && HasBearishSignal())
            //{
                //var Quantity = 1;

                //var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
                //ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, _initialStopLossInPips, _takeProfitInPips);
            //}
        }

        private double CalculatePositionQuantityInLots(double stopLossPips)
        {
            if (_lotSizingRule == LotSizingRuleValues.Static)
            {
                return 1;
            }

            var risk = Account.Equity * _dynamicRiskPercentage / 100;
            var oneLotRisk = Symbol.PipValue * stopLossPips * Symbol.LotSize;
            var quantity = Math.Round(risk / oneLotRisk, 1);

            Print("Account Equity={0}, Risk={1}, Risk for one lot based on SL of {2} = {3}, Qty = {4}",
                Account.Equity, risk, stopLossPips, oneLotRisk, quantity);

            return quantity;
        }

        protected override void OnTick()
        {
            if (_canOpenPosition)
                return;

            ManageExistingPosition();
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            CurrentPosition = args.Position;
            var sl = CurrentPosition.StopLoss.HasValue
                ? string.Format(" (SL={0})", CurrentPosition.StopLoss.Value)
                : string.Empty;

            var tp = CurrentPosition.TakeProfit.HasValue
                ? string.Format(" (TP={0})", CurrentPosition.TakeProfit.Value)
                : string.Empty;

            Print("{0} {1:N} at {2}{3}{4}", CurrentPosition.TradeType, CurrentPosition.VolumeInUnits, CurrentPosition.EntryPrice, sl, tp);
            _canOpenPosition = false;
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            Print("Closed {0:N} {1} at {2} for {3} profit", position.VolumeInUnits, position.TradeType, position.EntryPrice, position.GrossProfit);
            _canOpenPosition = true;
        }

        private void ManageExistingPosition()
        {
            switch (CurrentPosition.TradeType)
            {
                case TradeType.Buy:
                    ManageLongPosition();
                    break;

                case TradeType.Sell:
                    //ManageShortPosition();
                    break;
            }
        }

        protected abstract void ManageLongPosition();
    }
}
