﻿using cAlgo.API;
using System;
using System.Linq;

namespace Powder.TradingLibrary
{
    public abstract class BaseRobot : Robot
    {
        protected const int InitialRecentLow = int.MaxValue;
        protected const int InitialRecentHigh = 0;

        protected abstract string Name { get; }
        protected Position _currentPosition;
        protected double ExitPrice { get; private set; }
        protected int BarsSinceEntry { get; private set; }
        protected double RecentLow { get; set; }
        protected double RecentHigh { get; set; }
        protected bool ShouldTrail { get; set; }
        protected double BreakEvenPrice { get; private set; }
        protected double DoubleRiskPrice { get; private set; }
        protected double TripleRiskPrice { get; private set; }
        protected double? TrailingInitiationPrice { get; private set; }
        protected bool _canOpenPosition;
        protected InitialStopLossRuleValues _initialStopLossRule;

        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private TrailingStopLossRuleValues _trailingStopLossRule;
        private LotSizingRuleValues _lotSizingRule;
        private int _initialStopLossInPips;
        private TakeProfitRuleValues _takeProfitRule;
        private int _takeProfitInPips;
        private int _trailingStopLossInPips;
        private int _minutesToWaitAfterPositionClosed;
        private bool _moveToBreakEven;
        private bool _closeHalfAtBreakEven;
        private double _dynamicRiskPercentage;
        private int _barsToAllowTradeToDevelop;
        private DateTime _lastClosedPositionTime;
        private bool _alreadyMovedToBreakEven;
        private bool _isClosingHalf;
        private double? _entryStopLossInPips;
        private bool _onBar;

        protected virtual Weighting EntryWeighting { get; set; } = Weighting.Standard;

        protected abstract bool HasBullishSignal();
        protected abstract bool HasBearishSignal();

        protected void Init(
            bool takeLongsParameter,
            bool takeShortsParameter,
            InitialStopLossRuleValues initialStopLossRule,
            int initialStopLossInPips,
            TrailingStopLossRuleValues trailingStopLossRule,
            int trailingStopLossInPips,
            LotSizingRuleValues lotSizingRule,
            TakeProfitRuleValues takeProfitRule,
            int takeProfitInPips = 0,
            int minutesToWaitAfterPositionClosed = 0,
            bool moveToBreakEven = false,
            bool closeHalfAtBreakEven = false,
            double dynamicRiskPercentage = 2,
            int barsToAllowTradeToDevelop = 0)
        {
            ValidateParameters(takeLongsParameter, takeShortsParameter, initialStopLossRule, initialStopLossInPips,
                    trailingStopLossRule, trailingStopLossInPips, lotSizingRule, takeProfitRule, takeProfitInPips,
                    minutesToWaitAfterPositionClosed, moveToBreakEven, closeHalfAtBreakEven, dynamicRiskPercentage, barsToAllowTradeToDevelop);

            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = initialStopLossRule;
            _initialStopLossInPips = initialStopLossInPips;
            _trailingStopLossRule = trailingStopLossRule;
            _trailingStopLossInPips = trailingStopLossInPips;
            _lotSizingRule = lotSizingRule;
            _takeProfitRule = takeProfitRule;
            _takeProfitInPips = takeProfitInPips;
            _minutesToWaitAfterPositionClosed = minutesToWaitAfterPositionClosed;
            _moveToBreakEven = moveToBreakEven;
            _closeHalfAtBreakEven = closeHalfAtBreakEven;
            _dynamicRiskPercentage = dynamicRiskPercentage;
            _barsToAllowTradeToDevelop = barsToAllowTradeToDevelop;

            _canOpenPosition = true;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            Positions.Modified += OnPositionModified;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}",
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);

            AttachExistingPosition();
            _onBar = false;
        }

        protected override void OnStop()
        {
            // if we are backtesting then just close all the trades to simplify results
            if (!IsBacktesting)
            {
                return;
            }

            foreach (var position in Positions)
            {
                position.Close();
            }
        }

        private void AttachExistingPosition()
        {
            // If we have started the robot on a chart with an existing position then we want to manage this position
            var position = Positions.SingleOrDefault(p => p.SymbolName == Symbol.Name);
            if (position == null)
            {
                return;
            }

            if (_takeLongsParameter && position.TradeType == TradeType.Buy)
            {
                SetOpenPosition(position, printPositionInfo: false);
            }
            else if (_takeShortsParameter && position.TradeType == TradeType.Sell)
            {
                SetOpenPosition(position, printPositionInfo: false);
            }

            if (_currentPosition == null)
            {
                return;
            }

            CalcCorrectBarsSinceEntry();
        }

        private void CalcCorrectBarsSinceEntry()
        {
            var divisor = 0;

            if (Bars.TimeFrame.Equals(TimeFrame.Minute15))
            {
                divisor = 15;
            }
            else if (Bars.TimeFrame.Equals(TimeFrame.Minute5))
            {
                divisor = 5;
            }
            else if (Bars.TimeFrame.Equals(TimeFrame.Minute30))
            {
                divisor = 30;
            }
            else if (Bars.TimeFrame.Equals(TimeFrame.Hour))
            {
                divisor = 60;
            }
            else if (Bars.TimeFrame.Equals(TimeFrame.Hour4))
            {
                divisor = 240;
            }
            else
            {
                Print("Warning: Current timeframe unahandled for calculation of BarsSinceEntry on existing position.");
            }

            if (divisor != 0)
            {
                var minutes = Server.TimeInUtc.Subtract(_currentPosition.EntryTime).TotalMinutes;                
                BarsSinceEntry = Convert.ToInt32(Math.Floor(minutes / divisor));
                Print("BarsSinceEntry calculation: {0}, {1}", minutes / divisor, BarsSinceEntry);
            }
        }

        protected virtual void ValidateParameters(
            bool takeLongsParameter,
            bool takeShortsParameter,
            InitialStopLossRuleValues initialStopLossRule,
            int initialStopLossInPips,
            TrailingStopLossRuleValues trailingStopLossRule,
            int trailingStopLossInPips,
            LotSizingRuleValues lotSizingRule,
            TakeProfitRuleValues takeProfitRule,
            int takeProfitInPips,
            int minutesToWaitAfterPositionClosed,
            bool moveToBreakEven,
            bool closeHalfAtBreakEven,
            double dynamicRiskPercentage,
            int barsToAllowTradeToDevelop)
        {
            if (!takeLongsParameter && !takeShortsParameter)
                throw new ArgumentException("Must take at least longs or shorts");

            if (!Enum.IsDefined(typeof(InitialStopLossRuleValues), initialStopLossRule))
                throw new ArgumentException("Invalid initial stop loss rule");

            if (!Enum.IsDefined(typeof(TrailingStopLossRuleValues), trailingStopLossRule))
                throw new ArgumentException("Invalid trailing stop loss rule");

            if (initialStopLossInPips < 0 || initialStopLossInPips > 999)
                throw new ArgumentException("Invalid initial stop loss - must be between 0 and 999");

            if (trailingStopLossInPips < 0 || trailingStopLossInPips > 999)
                throw new ArgumentException("Invalid trailing stop loss - must be between 0 and 999");

            if (!Enum.IsDefined(typeof(LotSizingRuleValues), lotSizingRule))
                throw new ArgumentException("Invalid lot sizing rule");

            if (takeProfitInPips < 0 || takeProfitInPips > 999)
                throw new ArgumentException("Invalid take profit - must be between 0 and 999");

            if (!Enum.IsDefined(typeof(TakeProfitRuleValues), takeProfitRule))
                throw new ArgumentException("Invalid take profit rule");

            if (takeProfitRule != TakeProfitRuleValues.StaticPipsValue && takeProfitInPips != 0)
                throw new ArgumentException("Invalid take profit - must be 0 when Take Profit Rule is not Static Pips");

            if (minutesToWaitAfterPositionClosed < 0 || minutesToWaitAfterPositionClosed > 60 * 24)
                throw new ArgumentException(string.Format("Invalid 'Pause after position closed' - must be between 0 and {0}", 60 * 24));

            if (!moveToBreakEven && closeHalfAtBreakEven)
                throw new ArgumentException("'Close half at breakeven?' is only valid when 'Move to breakeven?' is set");

            if (lotSizingRule == LotSizingRuleValues.Dynamic && (dynamicRiskPercentage <= 0 || dynamicRiskPercentage > 5))
                throw new ArgumentOutOfRangeException("Dynamic Risk value is out of range - it is a percentage (e.g. 2) between 0 and 5");

            if (barsToAllowTradeToDevelop < 0 || barsToAllowTradeToDevelop > 99)
                throw new ArgumentOutOfRangeException("BarsToAllowTradeToDevelop is out of range - must be between 0 and 99");
        }

        protected override void OnTick()
        {
            if (_currentPosition == null)
                return;

            _onBar = false;
            ManageExistingPosition();
        }

        protected override void OnBar()
        {
            if (_currentPosition != null)
            {
                BarsSinceEntry++;
                //Print("Bars since entry: {0}", BarsSinceEntry);

                ModifyOppositeColourBarTrailingStop();
            }

            if (!_canOpenPosition || PendingOrders.Any())
                return;

            if (ShouldWaitBeforeLookingForNewSetup())
                return;

            if (_takeLongsParameter && HasBullishSignal())
            {
                EnterLongPosition();
            }
            else if (_takeShortsParameter && HasBearishSignal())
            {
                EnterShortPosition();
            }
        }

        private void ModifyOppositeColourBarTrailingStop()
        {
            if (_trailingStopLossRule != TrailingStopLossRuleValues.OppositeColourBar)
                return;

            _onBar = true;
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    ManageLongPosition();
                    break;

                case TradeType.Sell:
                    ManageShortPosition();
                    break;
            }
        }

        private void ManageExistingPosition()
        {
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    ManageLongPosition();
                    break;

                case TradeType.Sell:
                    ManageShortPosition();
                    break;
            }
        }

        /// <summary>
        /// Manages an existing long position.  Note this method is called on every tick.
        /// </summary>
        protected virtual bool ManageLongPosition()
        {
            if (BarsSinceEntry <= _barsToAllowTradeToDevelop)
                return false;

            if (_trailingStopLossRule == TrailingStopLossRuleValues.None && !_moveToBreakEven)
                return true;

            // Are we making higher highs?
            var madeNewHigh = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Ask >= BreakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForLongPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                }

                return true;
            }

            if (!ShouldTrail)
                return true;

            double? stop;
            if (_trailingStopLossRule == TrailingStopLossRuleValues.OppositeColourBar)
            {
                if (_onBar)
                {
                    stop = CalulateTrailingStopForLongPosition();
                    AdjustStopLossForLongPosition(stop);
                }

                return true;
            }

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent high {1}", Symbol.Bid, _recentHigh + buffer);
            if (Symbol.Ask > RecentHigh + buffer && _currentPosition.Pips > 0)
            {
                madeNewHigh = true;
                RecentHigh = Math.Max(Symbol.Ask, Bars.HighPrices.Maximum(BarsSinceEntry + 1));
                Print("Recent high set to {0}", RecentHigh);
            }

            if (!madeNewHigh)
                return true;

            stop = CalulateTrailingStopForLongPosition();
            AdjustStopLossForLongPosition(stop);

            return true;
        }

        protected void AdjustStopLossForLongPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value >= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        protected double? CalulateTrailingStopForLongPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRuleValues.StaticPipsValue:
                    stop = Symbol.Ask - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.CurrentBarNPips:
                    stop = Bars.LowPrices.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.PreviousBarNPips:
                    var low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(2));
                    stop = low - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.ShortTermHighLow:
                    stop = RecentHigh - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.SmartProfitLocker:
                    stop = CalculateSmartTrailingStopForLong();
                    break;

                case TrailingStopLossRuleValues.OppositeColourBar:
                    stop = CalculateOppositeColourBarTrailingStopForLong();
                    break;
            }

            return stop;
        }

        /// <summary>
        /// Manages an existing short position.  Note this method is called on every tick.
        /// </summary>
        protected virtual bool ManageShortPosition()
        {
            if (BarsSinceEntry <= _barsToAllowTradeToDevelop) return false;

            if (_trailingStopLossRule == TrailingStopLossRuleValues.None && !_moveToBreakEven) return true;

            // Are we making lower lows?
            var madeNewLow = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Bid <= BreakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForShortPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                }

                return true;
            }

            if (!ShouldTrail) return true;

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent low {1}", Symbol.Bid, _recentLow - buffer);
            if (Symbol.Bid < RecentLow - buffer && _currentPosition.Pips > 0)
            {
                madeNewLow = true;
                RecentLow = Math.Min(Symbol.Bid, Bars.LowPrices.Minimum(BarsSinceEntry + 1));
                Print("Recent low set to {0}", RecentLow);
            }

            if (!madeNewLow) return true;

            var stop = CalulateTrailingStopForShortPosition();
            AdjustStopLossForShortPosition(stop);

            return true;
        }

        private void AdjustStopLossForShortPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value <= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        protected double? CalulateTrailingStopForShortPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRuleValues.StaticPipsValue:
                    stop = Symbol.Bid + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.CurrentBarNPips:
                    stop = Bars.HighPrices.Last(1) + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.PreviousBarNPips:
                    var high = Math.Max(Bars.HighPrices.Last(1), Bars.HighPrices.Last(2));
                    stop = high + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.ShortTermHighLow:
                    stop = RecentLow + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRuleValues.SmartProfitLocker:
                    stop = CalculateSmartTrailingStopForShort();
                    break;
            }

            return stop;
        }

        private double? CalculateSmartTrailingStopForLong()
        {
            // This stop is designed to become tighter the more profit we make

            double initialRiskInPips = 20;
            if (_entryStopLossInPips.HasValue)
            {
                initialRiskInPips = _entryStopLossInPips.Value;
            }

            var onePointFiveProfitLevel = initialRiskInPips * 1.5;
            var doubleProfitLevel = initialRiskInPips * 2;
            var tripleProfitLevel = initialRiskInPips * 3;

            Print("initialRiskInPips = {0}", initialRiskInPips);
            double ratio;
            if (_currentPosition.Pips < initialRiskInPips)
            {
                Print("Lowest Band");
                ratio = initialRiskInPips;
            }
            else if (_currentPosition.Pips < onePointFiveProfitLevel)
            {
                Print("1-1.5 Band");
                ratio = initialRiskInPips * 3 / 4;
            }
            else if (_currentPosition.Pips < doubleProfitLevel)
            {
                Print("Double Band");
                ratio = initialRiskInPips / 2;
            }
            else if (_currentPosition.Pips < tripleProfitLevel)
            {
                Print("Triple Band");
                ratio = initialRiskInPips / 3;
            }
            else
            {
                Print("Highest Band");
                ratio = initialRiskInPips / 4;
            }

            ratio = Math.Truncate(ratio);
            Print("Ratio = {0}", ratio);

            var stop = RecentHigh - ratio * Symbol.PipSize;
            return stop;
        }

        private double? CalculateOppositeColourBarTrailingStopForLong()
        {
            // This method is called on a new bar
            if (Bars.ClosePrices.Last(1) >= Bars.OpenPrices.Last(1))
                return null;

            Print("Adjusting SL now we got a red candle");

            // We closed lower - adjust the stop to the the low of the previous bar
            return Bars.LowPrices.Minimum(2) - _trailingStopLossInPips * Symbol.PipSize;
        }

        private double? CalculateSmartTrailingStopForShort()
        {
            var minStop = 20;
            double stop;

            if (_currentPosition.Pips < minStop)
            {
                Print("Band 20");
                stop = minStop;
            }
            else if (_currentPosition.Pips < 40)
            {
                Print("Band 40");
                stop = 16;
            }
            else if (_currentPosition.Pips < 50)
            {
                Print("Band 50");
                stop = 12;
            }
            else
            {
                Print("Band MAX");
                stop = 8;
            }

            stop = RecentLow + stop * Symbol.PipSize;
            return stop;
        }

        private bool ShouldWaitBeforeLookingForNewSetup()
        {
            if (_minutesToWaitAfterPositionClosed > 0 &&
                _lastClosedPositionTime != DateTime.MinValue &&
                Server.Time.Subtract(_lastClosedPositionTime).TotalMinutes <= _minutesToWaitAfterPositionClosed)
            {
                Print("Pausing before we look for new opportunities.");
                return true;
            }

            // Alternately, avoid trading on a Friday evening
            var openTime = Bars.OpenTimes.LastValue;
            if (openTime.DayOfWeek == DayOfWeek.Friday && openTime.Hour >= 16)
            {
                Print("Avoiding trading on a Friday afternoon");
                return true;
            }

            return false;
        }

        protected virtual void EnterLongPosition()
        {
            var stopLossPips = CalculateInitialStopLossInPipsForLongPosition();
            double lots;

            if (stopLossPips.HasValue)
            {
                lots = CalculatePositionQuantityInLots(stopLossPips.Value);
                Print("SL calculated for Buy order = {0}", stopLossPips);
            }
            else
            {
                lots = 6;
            }

            _entryStopLossInPips = stopLossPips;
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);            
            ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
        }

        protected virtual double CalculatePositionQuantityInLots(double stopLossPips)
        {
            double riskPercentage = 0;
            switch (_lotSizingRule)
            {
                case LotSizingRuleValues.Static:
                    return 0.1;

                case LotSizingRuleValues.Dynamic:
                    riskPercentage = _dynamicRiskPercentage;
                    break;

                case LotSizingRuleValues.Weighted:
                    riskPercentage = _dynamicRiskPercentage * WeightingToRisk;
                    break;
            }

            // Safety check - we should never risk more than 5%!
            if (riskPercentage > 5)
            {
                riskPercentage = 5;
            }

            var risk = Account.Equity * riskPercentage / 100;
            var oneLotRisk = Symbol.PipValue * stopLossPips * Symbol.LotSize;
            var quantity = Math.Round(risk / oneLotRisk, 1);

            Print("Account Equity={0}, Risk={1}, Risk for one lot based on SL of {2} = {3}, Qty = {4}",
                Account.Equity, risk, stopLossPips, oneLotRisk, quantity);

            return quantity;
        }

        private double WeightingToRisk
        {
            get
            {
                if (EntryWeighting != Weighting.Standard)
                    Print( "Non standard weighting of {0}, therefore risk adjusted", EntryWeighting);

                switch (EntryWeighting)
                {
                    case Weighting.Standard:
                        return 1;
                    case Weighting.VeryWeak:
                        return 0.33;
                    case Weighting.Weak:
                        return 0.66;
                    case Weighting.Strong:
                        return 1.5;
                    case Weighting.VeryStrong:
                        return 2.5;

                    default:
                        return 1;
                }
            }
        }

        protected virtual double? CalculateInitialStopLossInPipsForLongPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case InitialStopLossRuleValues.None:
                    break;

                case InitialStopLossRuleValues.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case InitialStopLossRuleValues.CurrentBarNPips:
                    var low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(0));
                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
                    break;

                case InitialStopLossRuleValues.PreviousBarNPips:
                    low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(0));
                    if (Bars.LowPrices.Last(2) < low)
                    {
                        low = Bars.LowPrices.Last(2);
                    }

                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
                    break;
            }

            if (stopLossPips.HasValue)
            {
                return Math.Round(stopLossPips.Value, 1);
            }

            return null;
        }

        protected virtual double? CalculateTakeProfit(double? stopLossPips)
        {
            switch (_takeProfitRule)
            {
                case TakeProfitRuleValues.None:
                    return null;

                case TakeProfitRuleValues.StaticPipsValue:
                    return _takeProfitInPips;

                case TakeProfitRuleValues.DoubleRisk:
                    return stopLossPips.HasValue
                        ? stopLossPips.Value * 2
                        : (double?)null;

                case TakeProfitRuleValues.TripleRisk:
                    return stopLossPips.HasValue
                        ? stopLossPips.Value * 3
                        : (double?)null;

                default:
                    return null;
            }
        }

        protected virtual void EnterShortPosition()
        {
            var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();
            double lots;

            if (stopLossPips.HasValue)
            {
                lots = CalculatePositionQuantityInLots(stopLossPips.Value);
                Print("SL calculated for Sell order = {0}", stopLossPips);
            }
            else
            {
                lots = 1;
            }

            _entryStopLossInPips = stopLossPips;
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
            ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
        }

        protected virtual double? CalculateInitialStopLossInPipsForShortPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case InitialStopLossRuleValues.None:
                    break;

                case InitialStopLossRuleValues.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case InitialStopLossRuleValues.CurrentBarNPips:
                    var high = Math.Max(Bars.HighPrices.Last(1), Bars.HighPrices.Last(0));
                    stopLossPips = _initialStopLossInPips + (high - Symbol.Bid) / Symbol.PipSize;
                    break;

                case InitialStopLossRuleValues.PreviousBarNPips:
                    high = Math.Max(Bars.HighPrices.Last(1), Bars.HighPrices.Last(0));
                    if (Bars.HighPrices.Last(2) > high)
                    {
                        high = Bars.HighPrices.Last(2);
                    }

                    stopLossPips = _initialStopLossInPips + (high - Symbol.Bid) / Symbol.PipSize;
                    break;
            }

            if (stopLossPips.HasValue)
            {
                return Math.Round(stopLossPips.Value, 1);
            }

            return null;
        }

        protected virtual void OnPositionOpened(PositionOpenedEventArgs args)
        {
            SetOpenPosition(args.Position);
        }

        private void SetOpenPosition(Position position, bool printPositionInfo = true)
        {
            BarsSinceEntry = 0;
            RecentHigh = InitialRecentHigh;
            RecentLow = InitialRecentLow;
            _currentPosition = position;

            if (printPositionInfo)
            {
                var sl = position.StopLoss.HasValue
                    ? string.Format(" (SL={0})", position.StopLoss.Value)
                    : string.Empty;

                var tp = position.TakeProfit.HasValue
                    ? string.Format(" (TP={0})", position.TakeProfit.Value)
                    : string.Empty;

                Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);
            }

            CalculateBreakEvenPrice();
            CalculateDoubleRiskPrice();
            CalculateTripleRiskPrice();
            CalculateTrailingInitiationPrice();

            _canOpenPosition = false;
            ShouldTrail = true;
        }

        private void CalculateBreakEvenPrice()
        {
            //Print("Current position's SL = {0}", _currentPosition.StopLoss.HasValue
            //    ? _currentPosition.StopLoss.Value.ToString()
            //    : "N/A");

            if (!_currentPosition.StopLoss.HasValue)
            {
                return;
            }

            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        BreakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
                    }

                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        BreakEvenPrice = Symbol.Bid * 2 - _currentPosition.StopLoss.Value;
                    }

                    break;
            }
        }

        private void CalculateDoubleRiskPrice()
        {
            // Don't bother if we're never going to use it
            if (_takeProfitRule == TakeProfitRuleValues.DoubleRisk)
            {
                DoubleRiskPrice = CalculateRiskPrice(2);
            }
        }

        private void CalculateTripleRiskPrice()
        {
            // Don't bother if we're never going to use it
            if (_takeProfitRule == TakeProfitRuleValues.TripleRisk)
            {
                TripleRiskPrice = CalculateRiskPrice(3);
            }
        }

        private void CalculateTrailingInitiationPrice()
        {
            TrailingInitiationPrice = CalculateRiskPrice(0.75);
        }

        private double CalculateRiskPrice(double multiplier)
        {
            if (!_currentPosition.StopLoss.HasValue)
            {
                return 0;
            }

            double diff;
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        diff = _currentPosition.EntryPrice - _currentPosition.StopLoss.Value;
                        return _currentPosition.EntryPrice + (diff * multiplier);
                    }

                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        diff = _currentPosition.StopLoss.Value - _currentPosition.EntryPrice;
                        return _currentPosition.EntryPrice - (diff * multiplier);
                    }

                    break;
            }

            return 0;
        }

        protected virtual void OnPositionClosed(PositionClosedEventArgs args)
        {
            _currentPosition = null;
            _alreadyMovedToBreakEven = false;

            ExitPrice = CalculateExitPrice(args.Position);
            PrintClosedPositionInfo(args.Position);

            _lastClosedPositionTime = Server.Time;

            _canOpenPosition = true;
        }

        private void OnPositionModified(PositionModifiedEventArgs args)
        {
            if (!_isClosingHalf)
                return;

            ExitPrice = CalculateExitPrice(args.Position);
            PrintClosedPositionInfo(args.Position);
            _isClosingHalf = false;
        }

        private void PrintClosedPositionInfo(Position position)
        {
            Print("Closed {0:N} {1} at {2} for {3} profit (pips={4})",
                position.VolumeInUnits, position.TradeType, ExitPrice, position.GrossProfit, position.Pips);
        }

        private double CalculateExitPrice(Position position)
        {
            var diff = position.Pips * Symbol.PipSize;
            double exitPrice;
            if (position.TradeType == TradeType.Buy)
            {
                exitPrice = position.EntryPrice + diff;
            }
            else
            {
                exitPrice = position.EntryPrice - diff;
            }

            return exitPrice;
        }
    }
}
