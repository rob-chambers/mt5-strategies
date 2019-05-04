﻿using System;
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

        /*
         * bool CMyExpertBase::CheckToModifyPositions()
{
    if (_inpTrailingStopLossRule == None && !_inpMoveToBreakEven) return false;

    if (!_position.Select(Symbol())) {
        return false;
    }

    if (_position.PositionType() == POSITION_TYPE_BUY) {
        if (LongModified())
            return true;
    }
    else {
        if (ShortModified())
            return true;
    }

    return false;
}

bool CMyExpertBase::LongModified()
{
    double newStop = 0;

    //if (_barsSincePositionOpened == 0) {
    //    return false;
    //}

    // Are we making higher highs?
    if (_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh) {
        _recentHigh = _prices[1].high;
        _recentTurningPoint = _prices[1].low;
        _hadRecentTurningPoint = false;
    }
    else {        
        // Filter on _barsSincePositionOpened to give the position time to "breathe" (i.e. avoid moving SL too early after initial SL)
        if (_inpTrailingStopLossRule == ShortTermHighLow && !_hadRecentTurningPoint) {

            // For this SL rule we only operate after a new bar forms
            if (IsNewBar(iTime(0))) {
                _barsSincePositionOpened++;
                //Print("New bar found: ", _barsSincePositionOpened);
            }
            else {
                return false;
            }

            if (_barsSincePositionOpened < 3) return false;

            if (_prices[1].low < _recentTurningPoint && _prices[1].high < _recentHigh) {
                //Print("STH found: ", _recentHigh);

                // We have a short term high (STH).  Set SL to the low of the STH bar plus a margin
                newStop = _recentTurningPoint - _adjustedPoints * 6;
                _hadRecentTurningPoint = true;
            }
            else {
                // No new STH - nothing to do
                return false;
            }
        }
        else {
            return false;
        }
    }

    double breakEvenPoint = 0;
    //if (!_trailingStarted) {
    double initialRisk = _position.PriceOpen() - _initialStop;
    breakEvenPoint = _position.PriceOpen() + initialRisk;

    //    if (_currentAsk <= breakEvenPoint) {
    //        return false;
    //    }

    //    Print("Initiating trailing as we have hit breakeven");
    //    _trailingStarted = true;
    //}

    switch (_inpTrailingStopLossRule) {
        case StaticPipsValue:
            newStop = _recentHigh - _trailing_stop;
            break;

        case CurrentBar2Pips:
            newStop = _prices[1].low - _adjustedPoints * 2;
            break;

        case CurrentBar5Pips:
            newStop = _prices[1].low - _adjustedPoints * 5;
            break;

        case CurrentBar2ATR:
            newStop = _currentAsk - _atrData[0] * 2;
            break;

        case PreviousBar5Pips:
            // TODO: Check if previous bar high is actually higher!


            newStop = _prices[2].low - _adjustedPoints * 5;
            break;

        case PreviousBar2Pips:
            // TODO: Check if previous bar high is actually higher!
            newStop = _prices[2].low - _adjustedPoints * 2;
            break;
    }

    // TOOD: Is the new stop sufficiently far away or perhaps too far?


    // Check if we should move to breakeven
    //double risk;
    if (_inpMoveToBreakEven && !_alreadyMovedToBreakEven) {
        //risk = _position.PriceOpen() - _initialStop;
        //double breakEvenPoint = _position.PriceOpen() + risk;
        //    
        if (_currentAsk > breakEvenPoint) {
            if (newStop == 0.0 || breakEvenPoint > newStop) {
                printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);
                newStop = _position.PriceOpen();
            }
        }
    }

    if (newStop == 0.0) {
        return false;
    }

    double sl = NormalizeDouble(newStop, _symbol.Digits());
    double tp = _position.TakeProfit();        
    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

    if (_position.StopLoss() < sl || _position.StopLoss() == 0.0) {
        double diff = (_currentAsk - sl) / _adjustedPoints;
        if (diff < stopLevelPips) {
            printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
                _currentAsk, sl, stopLevelPips, diff);

            sl = _currentAsk - stopLevelPips * _adjustedPoints;
        }

        //--- modify position
        if (!_trade.PositionModify(Symbol(), sl, tp)) {
            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f", sl, tp);
        }

        if (!_alreadyMovedToBreakEven && sl >= _position.PriceOpen()) {
            int profitInPips = int((sl - _position.PriceOpen()) / _adjustedPoints);
            printf("%d pips profit now locked in (sl = %f, open = %f)", profitInPips, sl, _position.PriceOpen());
            _alreadyMovedToBreakEven = true;
        }            

        return true;
    }

    return false;
}
         */

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

                Print("Cross identified at index {0}", lastCross);

                //var close = MarketSeries.Close;
                //Print("Close = {0}, 0 Close = {1}, 1 Close = {2}", close.LastValue, close.Last(0), close.Last(1));

                //var high = MarketSeries.High;
                //Print("High = {0}, 0 High = {1}, 1 High = {2}", high.LastValue, high.Last(0), high.Last(1));

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

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
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

        protected override bool HasBearishSignal()
        {
            var currentClose = MarketSeries.Close.Last(1);

            if (currentClose < _slowMA.Result.LastValue
                && currentClose < _mediumMA.Result.LastValue
                && currentClose < _fastMA.Result.LastValue)
            {
                return true;
            }

            return false;
        }

        protected override void ManageLongPosition()
        {
            // Important - call base funcitonality to trail stop higher
            base.ManageLongPosition();

            /* RULES
             * 1) If we close below the fast MA, we close the position.  Add a small 2 pip buffer
             */
            if (MarketSeries.Close.Last(1) < _fastMA.Result.LastValue - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the fast MA");
                _currentPosition.Close();
            }

            //// Trail the stop up based on the new high
            //const int TrailingStopPips = 10;
            //var stop = _recentHigh - TrailingStopPips * Symbol.PipSize;

            //Print("Adjusting stop loss based on new high of {0}", _recentHigh);
            //ModifyPosition(_currentPosition, stop, _currentPosition.TakeProfit);
        }
    }

    public abstract class BaseRobot : Robot
    {
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
        private double _targetx1;
        private double _breakEvenPrice;
        private bool _isClosingHalf;

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

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            Positions.Modified += OnPositionModified;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
        }

        protected override void OnTick()
        {
            if (_currentPosition != null)
            {
                ManageExistingPosition();
                return;
            }
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

            // Trail the stop up based on the new high
            var stop = CalulateTrailingStopForLongPosition();

            //var stop = _recentHigh - _trailingStopLossInPips * Symbol.PipSize;

            Print("Adjusting stop loss to {0} based on new high of {1}", stop, _recentHigh);
            AdjustStopLossForLongPosition(stop);
        }

        private void AdjustStopLossForLongPosition(double newStop)
        {
            if (_currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value > newStop)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double CalulateTrailingStopForLongPosition()
        {
            double stop = 0;
            switch (_trailingStopLossRule)
            {
                case StopLossRule.StaticPipsValue:
                    stop = _trailingStopLossInPips;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stop = MarketSeries.Low.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    stop = MarketSeries.Low.Last(2) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case StopLossRule.ShortTermHighLow:
                    stop = _recentHigh - _trailingStopLossInPips * Symbol.PipSize;
                    break;
            }

            return stop;
        }

        private void EnterLongPosition()
        {
            var Quantity = 1;

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
            var stopLossPips = CalculateStopLossInPipsForBuyOrder();            

            if (stopLossPips.HasValue)
            {
                Print("SL calculated for Buy order = {0}", stopLossPips);
                _targetx1 = MarketSeries.Close.LastValue + stopLossPips.Value * Symbol.PipSize;
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit());
            }
            else
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name);
            }
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
            ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, _initialStopLossInPips, _takeProfitInPips);
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
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    //Print("Current position's SL = {0}", _currentPosition.StopLoss.HasValue
                    //    ? _currentPosition.StopLoss.Value.ToString()
                    //    : "N/A");

                    if (_currentPosition.StopLoss.HasValue)
                    {
                        _breakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
                    }
                    
                    break;
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            _currentPosition = null;
            _recentHigh = 0;
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

        private double? CalculateStopLossInPipsForBuyOrder()
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

            return stopLossPips.HasValue
                ? (double?)Math.Round(stopLossPips.Value)
                : null;
        }
    }
}