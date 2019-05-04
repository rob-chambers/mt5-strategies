using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Library.Robots.WaveCatcher
{
    public enum StopLossRule
    {
        None,
        StaticPipsValue,
        //CurrentBar2ATR,
        CurrentBarNPips,
        PreviousBarNPips,
        ShortTermHighLow
    };

    public enum LotSizingRule
    {
        Static,
        Dynamic
    };

    public enum MaCrossRule
    {
        None,
        CloseOnFastMaCross,
        CloseOnMediumMaCross
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class WaveCatcherBot : BaseRobot
    {
        #region Standard Parameters
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = "StaticPipsValue")]
        public string InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = "None")]
        public string TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = "Static")]
        public string LotSizingRule { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 60)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Pause after position closed (Minutes)", DefaultValue = 0)]
        public int MinutesToWaitAfterPositionClosed { get; set; }

        [Parameter("Move to breakeven?", DefaultValue = false)]
        public bool MoveToBreakEven { get; set; }

        [Parameter("Close half at breakeven?", DefaultValue = false)]
        public bool CloseHalfAtBreakEven { get; set; }
        #endregion

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("MAs Cross Threshold (# bars)", DefaultValue = 10)]
        public int MovingAveragesCrossThreshold { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = "CloseOnFastMaCross")]
        public string MaCrossRule { get; set; }

        protected override string Name
        {
            get
            {
                return "WaveCatcher";
            }
        }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private AverageTrueRange _atr;
        private MaCrossRule _maCrossRule;

        protected override void OnStart()
        {
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Initial SL in pips: {0}", InitialStopLossInPips);
            Print("Trailing SL rule: {0}", TrailingStopLossRule);
            Print("Trailing SL in pips: {0}", TrailingStopLossInPips);
            Print("Lot sizing rule: {0}", LotSizingRule);
            Print("Take profit in pips: {0}", TakeProfitInPips);
            Print("Minutes to wait after position closed: {0}", MinutesToWaitAfterPositionClosed);
            Print("Move to breakeven: {0}", MoveToBreakEven);
            Print("Close half at breakeven: {0}", CloseHalfAtBreakEven);
            Print("MA Cross Rule: {0}", MaCrossRule);
            _maCrossRule = (MaCrossRule)Enum.Parse(typeof(MaCrossRule), MaCrossRule);

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
                TakeProfitInPips,                
                MinutesToWaitAfterPositionClosed,
                MoveToBreakEven,
                CloseHalfAtBreakEven);
        }

        protected override bool HasBullishSignal()
        {
            /* RULES
            1) Fast MA > Medium MA > Slow MA (MAs are 'stacked')
            2) Crossing of MAs must have occurred in the last n bars
            3) Close > Fast MA
            4) Close > Yesterday's close
            5) Close > Open
            6) High > Yesterday's high
             */
            if (AreMovingAveragesStackedBullishly())
            {
                var lastCross = GetLastBullishBowtie();
                if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
                {
                    // Either there was no cross or it was too long ago and we have missed the move
                    return false;
                }

                Print("Bullish cross identified at index {0}", lastCross);

                if (MarketSeries.Close.LastValue <= _fastMA.Result.LastValue)
                {
                    Print("Setup rejected as we closed lower than the fast MA");
                    return false;
                }

                if (MarketSeries.Close.Last(1) <= MarketSeries.Close.Last(2))
                {
                    Print("Setup rejected as we closed lower than the prior close ({0} vs {1})",
                        MarketSeries.Close.Last(1), MarketSeries.Close.Last(2));
                    return false;
                }

                if (MarketSeries.Close.Last(1) <= MarketSeries.Open.Last(1))
                {
                    Print("Setup rejected as we closed lower than the open ({0} vs {1})",
                        MarketSeries.Close.Last(1), MarketSeries.Open.Last(1));
                    return false;
                }

                if (MarketSeries.High.Last(1) <= MarketSeries.High.Last(2))
                {
                    Print("Setup rejected as the high wasn't higher than the prior high ({0} vs {1})",
                        MarketSeries.High.Last(1), MarketSeries.High.Last(2));
                    return false;
                }

                return true;
            }

            return false;
        }

        private bool AreMovingAveragesStackedBullishly()
        {
            return _fastMA.Result.LastValue > _mediumMA.Result.LastValue &&
                _mediumMA.Result.LastValue > _slowMA.Result.LastValue;
        }

        private bool AreMovingAveragesStackedBearishly()
        {
            return _fastMA.Result.LastValue < _mediumMA.Result.LastValue &&
                _mediumMA.Result.LastValue < _slowMA.Result.LastValue;
        }

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
        {
            return _fastMA.Result.Last(index) > _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) > _slowMA.Result.Last(index);
        }

        private bool AreMovingAveragesStackedBearishlyAtIndex(int index)
        {
            return _fastMA.Result.Last(index) > _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) > _slowMA.Result.Last(index);
        }

        private int GetLastBullishBowtie()
        {
            if (!AreMovingAveragesStackedBullishly())
            {
                return -1;
            }

            var index = 1;
            while (index <= 30)
            {
                if (AreMovingAveragesStackedBullishlyAtIndex(index))
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }

            return -1;
        }

        private int GetLastBearishBowtie()
        {
            if (!AreMovingAveragesStackedBearishly())
            {
                return -1;
            }

            var index = 1;
            while (index <= 30)
            {
                if (AreMovingAveragesStackedBearishlyAtIndex(index))
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }

            return -1;
        }

        protected override bool HasBearishSignal()
        {
            /* RULES
            1) Fast MA < Medium MA < Slow MA (MAs are 'stacked')
            2) Crossing of MAs must have occurred in the last n bars
            3) Close < Fast MA
            4) Close < Yesterday's close
            5) Close < Open
            6) Low < Yesterday's low
             */
            if (AreMovingAveragesStackedBearishly())
            {
                var lastCross = GetLastBearishBowtie();
                if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
                {
                    // Either there was no cross or it was too long ago and we have missed the move
                    return false;
                }

                Print("Bearish cross identified at index {0}", lastCross);

                if (MarketSeries.Close.LastValue >= _fastMA.Result.LastValue)
                {
                    Print("Setup rejected as we closed higher than the fast MA");
                    return false;
                }

                if (MarketSeries.Close.Last(1) >= MarketSeries.Close.Last(2))
                {
                    Print("Setup rejected as we closed higher than the prior close ({0} vs {1})",
                        MarketSeries.Close.Last(1), MarketSeries.Close.Last(2));
                    return false;
                }

                if (MarketSeries.Close.Last(1) >= MarketSeries.Open.Last(1))
                {
                    Print("Setup rejected as we closed higher than the open ({0} vs {1})",
                        MarketSeries.Close.Last(1), MarketSeries.Open.Last(1));
                    return false;
                }

                if (MarketSeries.Low.Last(1) >= MarketSeries.Low.Last(2))
                {
                    Print("Setup rejected as the low wasn't lower than the prior low ({0} vs {1})",
                        MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
                    return false;
                }

                return true;
            }

            return false;
        }

        protected override void ManageLongPosition()
        {
            // Important - call base functionality to trail stop higher
            base.ManageLongPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case WaveCatcher.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case WaveCatcher.MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return;
            }

            if (MarketSeries.Close.Last(1) < value - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the {0} MA", maType);
                _currentPosition.Close();
            }
        }

        protected override void ManageShortPosition()
        {
            // Important - call base functionality to trail stop lower
            base.ManageShortPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case WaveCatcher.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case WaveCatcher.MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return;
            }

            if (MarketSeries.Close.Last(1) > value + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the {0} MA", maType);
                _currentPosition.Close();
            }
        }
    }

    public abstract class BaseRobot : Robot
    {
        private const int _initialRecentLow = int.MaxValue;

        protected abstract string Name { get; }
        protected Position _currentPosition;

        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private StopLossRule _initialStopLossRule;
        private StopLossRule _trailingStopLossRule;
        private LotSizingRule _lotSizingRule;
        private int _initialStopLossInPips;
        private int _takeProfitInPips;
        private int _trailingStopLossInPips;
        private int _minutesToWaitAfterPositionClosed;
        private bool _moveToBreakEven;
        private bool _closeHalfAtBreakEven;
        private bool _canOpenPosition;
        private DateTime _lastClosedPositionTime;
        private double _takeProfitLevel;
        private double _recentHigh;
        private bool _inpMoveToBreakEven;
        private bool _alreadyMovedToBreakEven;
        private double _breakEvenPrice;
        private bool _isClosingHalf;
        private double _recentLow;

        protected abstract bool HasBullishSignal();
        protected abstract bool HasBearishSignal();

        protected void Init(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            string initialStopLossRule,
            int initialStopLossInPips,
            string trailingStopLossRule,
            int trailingStopLossInPips,
            string lotSizingRule,            
            int takeProfitInPips = 0,            
            int minutesToWaitAfterPositionClosed = 0,
            bool moveToBreakEven = false,
            bool closeHalfAtBreakEven = false)
        {
            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), initialStopLossRule);
            _initialStopLossInPips = initialStopLossInPips;
            _trailingStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), trailingStopLossRule);
            _trailingStopLossInPips = trailingStopLossInPips;
            _lotSizingRule = (LotSizingRule)Enum.Parse(typeof(LotSizingRule), lotSizingRule);            
            _takeProfitInPips = takeProfitInPips;
            _minutesToWaitAfterPositionClosed = minutesToWaitAfterPositionClosed;
            _moveToBreakEven = moveToBreakEven;
            _closeHalfAtBreakEven = closeHalfAtBreakEven;

            _canOpenPosition = true;
            _recentHigh = 0;
            _recentLow = _initialRecentLow;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            Positions.Modified += OnPositionModified;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
        }

        protected override void OnTick()
        {
            if (_currentPosition == null)
                return;

            ManageExistingPosition();                
        }

        protected override void OnBar()
        {
            if (!_canOpenPosition || PendingOrders.Count > 0)
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
        protected virtual void ManageLongPosition()
        {
            if (_trailingStopLossRule == StopLossRule.None && !_moveToBreakEven)
                return;

            // Are we making higher highs?
            var madeNewHigh = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Ask >= _breakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForLongPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);                    
                }

                return;
            }

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent high {1}", Symbol.Bid, _recentHigh + buffer);
            if (Symbol.Ask > _recentHigh + buffer)
            {
                madeNewHigh = true;
                _recentHigh = Symbol.Ask;
            }

            if (!madeNewHigh)
            {
                return;
            }

            var stop = CalulateTrailingStopForLongPosition();
            AdjustStopLossForLongPosition(stop);
        }

        private void AdjustStopLossForLongPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value >= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalulateTrailingStopForLongPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case StopLossRule.StaticPipsValue:
                    stop = Symbol.Ask - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stop = MarketSeries.Low.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    var low = Math.Min(MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
                    stop = low - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.ShortTermHighLow:
                    stop = _recentHigh - _trailingStopLossInPips * Symbol.PipSize;
                    break;
            }

            return stop;
        }        

        /// <summary>
        /// Manages an existing short position.  Note this method is called on every tick.
        /// </summary>
        protected virtual void ManageShortPosition()
        {
            if (_trailingStopLossRule == StopLossRule.None && !_moveToBreakEven)
                return;

            // Are we making lower lows?
            var madeNewLow = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Bid <= _breakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForShortPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                }

                return;
            }

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent low {1}", Symbol.Bid, _recentLow - buffer);
            if (Symbol.Bid < _recentLow - buffer)
            {
                madeNewLow = true;
                _recentLow = Symbol.Bid;
            }

            if (!madeNewLow)
            {
                return;
            }

            var stop = CalulateTrailingStopForShortPosition();
            AdjustStopLossForShortPosition(stop);
        }

        private void AdjustStopLossForShortPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value <= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalulateTrailingStopForShortPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case StopLossRule.StaticPipsValue:
                    stop = Symbol.Bid + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stop = MarketSeries.High.Last(1) + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    var high = Math.Max(MarketSeries.High.Last(1), MarketSeries.High.Last(2));
                    stop = high + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.ShortTermHighLow:
                    stop = _recentLow + _trailingStopLossInPips * Symbol.PipSize;
                    break;
            }

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

            return false;
        }

        private void EnterLongPosition()
        {
            var Quantity = 1;

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
            var stopLossPips = CalculateInitialStopLossInPipsForLongPosition();            

            if (stopLossPips.HasValue)
            {
                Print("SL calculated for Buy order = {0}", stopLossPips);
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit());
            }
            else
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name);
            }
        }

        private double? CalculateInitialStopLossInPipsForLongPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case StopLossRule.None:
                    break;

                case StopLossRule.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    var low = MarketSeries.Low.Last(1);
                    if (MarketSeries.Low.Last(2) < low)
                    {
                        low = MarketSeries.Low.Last(2);
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

        private double? CalculateTakeProfit()
        {
            return _takeProfitInPips == 0 
                ? (double?)null 
                : _takeProfitInPips;
        }

        private void EnterShortPosition()
        {            
            var Quantity = 1;
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
            var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();

            if (stopLossPips.HasValue)
            {
                Print("SL calculated for Sell order = {0}", stopLossPips);
                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit());
            }
            else
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name);
            }
        }

        private double? CalculateInitialStopLossInPipsForShortPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case StopLossRule.None:
                    break;

                case StopLossRule.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stopLossPips = _initialStopLossInPips + (MarketSeries.High.Last(1) - Symbol.Bid) / Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    var high = MarketSeries.High.Last(1);
                    if (MarketSeries.High.Last(2) > high)
                    {
                        high = MarketSeries.High.Last(2);
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

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            _currentPosition = args.Position;
            var position = args.Position;
            var sl = position.StopLoss.HasValue
                ? string.Format(" (SL={0})", position.StopLoss.Value)
                : string.Empty;

            var tp = position.TakeProfit.HasValue
                ? string.Format(" (TP={0})", position.TakeProfit.Value)
                : string.Empty;

            Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);

            CalculateBreakEvenPrice();

            _canOpenPosition = false;
        }

        private void CalculateBreakEvenPrice()
        {
            //Print("Current position's SL = {0}", _currentPosition.StopLoss.HasValue
            //    ? _currentPosition.StopLoss.Value.ToString()
            //    : "N/A");
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        _breakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
                    }
                    
                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        _breakEvenPrice = Symbol.Bid * 2 - _currentPosition.StopLoss.Value;
                    }

                    break;
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            _currentPosition = null;
            _recentHigh = 0;
            _recentLow = _initialRecentLow;
            _alreadyMovedToBreakEven = false;
            PrintClosedPositionInfo(args.Position);

            _lastClosedPositionTime = Server.Time;

            _canOpenPosition = true;
        }
        
        private void OnPositionModified(PositionModifiedEventArgs args)
        {
            if (!_isClosingHalf)
                return;

            PrintClosedPositionInfo(args.Position);
            _isClosingHalf = false;
        }

        private void PrintClosedPositionInfo(Position position)
        {
            Print("Closed {0:N} {1} at {2} for {3} profit",
                position.VolumeInUnits, position.TradeType, position.EntryPrice, position.GrossProfit);
        }
    }
}
