//using System;
//using cAlgo.API;
//using cAlgo.API.Indicators;
//using cAlgo.API.Internals;

//namespace cAlgo.Library.Robots.BreakOutBot
//{
//    public enum StopLossRule
//    {
//        None,
//        StaticPipsValue,
//        //CurrentBar2ATR,
//        CurrentBarNPips,
//        PreviousBarNPips,
//        ShortTermHighLow
//    };

//    public enum LotSizingRule
//    {
//        Static,
//        Dynamic
//    };

//    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
//    public class BreakOutBot : BaseRobot
//    {
//        [Parameter("Take long trades?", DefaultValue = true)]
//        public bool TakeLongsParameter { get; set; }

//        [Parameter("Take short trades?", DefaultValue = false)]
//        public bool TakeShortsParameter { get; set; }

//        [Parameter("Initial SL Rule", DefaultValue = "CurrentBarNPips")]
//        public string InitialStopLossRule { get; set; }

//        [Parameter("Trailing SL Rule", DefaultValue = "None")]
//        public string TrailingStopLossRule { get; set; }

//        [Parameter("Lot Sizing Rule", DefaultValue = "Static")]
//        public string LotSizingRule { get; set; }

//        [Parameter()]
//        public DataSeries SourceSeries { get; set; }

//        [Parameter("Slow MA Period", DefaultValue = 240)]
//        public int SlowPeriodParameter { get; set; }

//        [Parameter("Medium MA Period", DefaultValue = 100)]
//        public int MediumPeriodParameter { get; set; }

//        [Parameter("Fast MA Period", DefaultValue = 50)]
//        public int FastPeriodParameter { get; set; }

//        [Parameter("Initial SL (pips)", DefaultValue = 5)]
//        public int InitialStopLossInPips { get; set; }

//        [Parameter("Take Profit (pips)", DefaultValue = 50)]
//        public int TakeProfitInPips { get; set; }

//        protected override string Name
//        {
//            get
//            {
//                return "Basic cBot";
//            }
//        }

//        private MovingAverage _fastMA;
//        private MovingAverage _mediumMA;
//        private MovingAverage _slowMA;
//        private AverageTrueRange _atr;

//        protected override void OnStart()
//        {
//            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
//            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
//            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Weighted);
//            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

//            Print("Take Longs: {0}", TakeLongsParameter);
//            Print("Take Shorts: {0}", TakeShortsParameter);
//            Print("Initial SL rule: {0}", InitialStopLossRule);
//            Print("Trailing SL rule: {0}", TrailingStopLossRule);

//            Init(TakeLongsParameter, 
//                TakeShortsParameter,
//                InitialStopLossRule,
//                TrailingStopLossRule,
//                LotSizingRule,
//                InitialStopLossInPips,
//                TakeProfitInPips);
//        }

//        /*
//         * bool CMyExpertBase::CheckToModifyPositions()
//{
//    if (_inpTrailingStopLossRule == None && !_inpMoveToBreakEven) return false;

//    if (!_position.Select(Symbol())) {
//        return false;
//    }

//    if (_position.PositionType() == POSITION_TYPE_BUY) {
//        if (LongModified())
//            return true;
//    }
//    else {
//        if (ShortModified())
//            return true;
//    }

//    return false;
//}

//bool CMyExpertBase::LongModified()
//{
//    double newStop = 0;

//    //if (_barsSincePositionOpened == 0) {
//    //    return false;
//    //}

//    // Are we making higher highs?
//    if (_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh) {
//        _recentHigh = _prices[1].high;
//        _recentTurningPoint = _prices[1].low;
//        _hadRecentTurningPoint = false;
//    }
//    else {        
//        // Filter on _barsSincePositionOpened to give the position time to "breathe" (i.e. avoid moving SL too early after initial SL)
//        if (_inpTrailingStopLossRule == ShortTermHighLow && !_hadRecentTurningPoint) {

//            // For this SL rule we only operate after a new bar forms
//            if (IsNewBar(iTime(0))) {
//                _barsSincePositionOpened++;
//                //Print("New bar found: ", _barsSincePositionOpened);
//            }
//            else {
//                return false;
//            }

//            if (_barsSincePositionOpened < 3) return false;

//            if (_prices[1].low < _recentTurningPoint && _prices[1].high < _recentHigh) {
//                //Print("STH found: ", _recentHigh);

//                // We have a short term high (STH).  Set SL to the low of the STH bar plus a margin
//                newStop = _recentTurningPoint - _adjustedPoints * 6;
//                _hadRecentTurningPoint = true;
//            }
//            else {
//                // No new STH - nothing to do
//                return false;
//            }
//        }
//        else {
//            return false;
//        }
//    }

//    double breakEvenPoint = 0;
//    //if (!_trailingStarted) {
//    double initialRisk = _position.PriceOpen() - _initialStop;
//    breakEvenPoint = _position.PriceOpen() + initialRisk;

//    //    if (_currentAsk <= breakEvenPoint) {
//    //        return false;
//    //    }

//    //    Print("Initiating trailing as we have hit breakeven");
//    //    _trailingStarted = true;
//    //}

//    switch (_inpTrailingStopLossRule) {
//        case StaticPipsValue:
//            newStop = _recentHigh - _trailing_stop;
//            break;

//        case CurrentBar2Pips:
//            newStop = _prices[1].low - _adjustedPoints * 2;
//            break;

//        case CurrentBar5Pips:
//            newStop = _prices[1].low - _adjustedPoints * 5;
//            break;

//        case CurrentBar2ATR:
//            newStop = _currentAsk - _atrData[0] * 2;
//            break;

//        case PreviousBar5Pips:
//            // TODO: Check if previous bar high is actually higher!


//            newStop = _prices[2].low - _adjustedPoints * 5;
//            break;

//        case PreviousBar2Pips:
//            // TODO: Check if previous bar high is actually higher!
//            newStop = _prices[2].low - _adjustedPoints * 2;
//            break;
//    }

//    // TOOD: Is the new stop sufficiently far away or perhaps too far?


//    // Check if we should move to breakeven
//    //double risk;
//    if (_inpMoveToBreakEven && !_alreadyMovedToBreakEven) {
//        //risk = _position.PriceOpen() - _initialStop;
//        //double breakEvenPoint = _position.PriceOpen() + risk;
//        //    
//        if (_currentAsk > breakEvenPoint) {
//            if (newStop == 0.0 || breakEvenPoint > newStop) {
//                printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);
//                newStop = _position.PriceOpen();
//            }
//        }
//    }

//    if (newStop == 0.0) {
//        return false;
//    }

//    double sl = NormalizeDouble(newStop, _symbol.Digits());
//    double tp = _position.TakeProfit();        
//    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

//    if (_position.StopLoss() < sl || _position.StopLoss() == 0.0) {
//        double diff = (_currentAsk - sl) / _adjustedPoints;
//        if (diff < stopLevelPips) {
//            printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
//                _currentAsk, sl, stopLevelPips, diff);

//            sl = _currentAsk - stopLevelPips * _adjustedPoints;
//        }

//        //--- modify position
//        if (!_trade.PositionModify(Symbol(), sl, tp)) {
//            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
//            printf("Modify parameters : SL=%f,TP=%f", sl, tp);
//        }

//        if (!_alreadyMovedToBreakEven && sl >= _position.PriceOpen()) {
//            int profitInPips = int((sl - _position.PriceOpen()) / _adjustedPoints);
//            printf("%d pips profit now locked in (sl = %f, open = %f)", profitInPips, sl, _position.PriceOpen());
//            _alreadyMovedToBreakEven = true;
//        }            

//        return true;
//    }

//    return false;
//}
//         */

//        protected override bool HasBullishSignal()
//        {
//            /* RULES
//            1) Close near the high
//            2) Open near the low
//            3) Range > 2*ATR
//            4) High must be higher than last 30 bars
            
//            Buy at market with SL = 30 pips

//            * Maintaining position - 
//            Don't lose more than half max profit
//            Set trailing stop to high - 10 pips
//             */
//            if (OpenNearLow() && CloseNearHigh() && IsSignificantBar())
//            {
//                const int HighPeriod = 30;

//                var currentHigh = MarketSeries.High.Last(1);
//                if (currentHigh >= MarketSeries.High.Maximum(HighPeriod))
//                {
//                    Print("Found a high over the last {0} bars", HighPeriod);
//                    return true;
//                }
//            }

//            return false;
//        }

//        private bool OpenNearLow()
//        {
//            var open = MarketSeries.Open.Last(1);
//            var currentLow = MarketSeries.Low.Last(1);

//            return (open - currentLow) / Symbol.PipSize < 3;
//        }

//        private bool CloseNearHigh()
//        {
//            var currentHigh = MarketSeries.High.Last(1);
//            var currentClose = MarketSeries.Close.Last(1);

//            return ((currentHigh - currentClose) / Symbol.PipSize) < 3;
//        }

//        private bool IsSignificantBar()
//        {
//            var range = MarketSeries.High.Last(1) - MarketSeries.Low.Last(1);
//            var rangeInPips = range / Symbol.PipSize;
//            var atrInPips = _atr.Result.LastValue / Symbol.PipSize;

//            return rangeInPips > 2 * atrInPips;
//        }

//        protected override bool HasBearishSignal()
//        {
//            var currentClose = MarketSeries.Close.Last(1);

//            if (currentClose < _slowMA.Result.LastValue
//                && currentClose < _mediumMA.Result.LastValue
//                && currentClose < _fastMA.Result.LastValue)
//            {
//                return true;
//            }

//            return false;
//        }

//        private bool IsBullishPinBar(int index)
//        {
//            const double PinbarThreshhold = 0.67;

//            var lastBarIndex = index - 1;
//            var priorBarIndex = lastBarIndex - 1;
//            var currentLow = MarketSeries.Low[lastBarIndex];
//            var priorLow = MarketSeries.Low[priorBarIndex];
//            var currentHigh = MarketSeries.High[lastBarIndex];
//            var priorHigh = MarketSeries.High[priorBarIndex];
//            var close = MarketSeries.Close[lastBarIndex];
//            var currentOpen = MarketSeries.Open[lastBarIndex];

//            if (!(currentLow < priorLow)) return false;
//            if (!(close > priorLow)) return false;
//            if (currentHigh > priorHigh) return false;

//            var closeFromHigh = currentHigh - close;
//            var openFromHigh = currentHigh - currentOpen;
//            var range = currentHigh - currentLow;

//            if (!((closeFromHigh / range <= (1 - PinbarThreshhold)) &&
//                (openFromHigh / range <= (1 - PinbarThreshhold))))
//            {
//                return false;
//            }

//            // Check length of pin bar
//            if (range < _atr.Result[lastBarIndex])
//            {
//                return false;
//            }

//            return true;
//        }
//    }

//    public abstract class BaseRobot : Robot
//    {
//        protected Position _currentPosition;

//        private bool _takeLongsParameter;
//        private bool _takeShortsParameter;
//        private StopLossRule _initialStopLossRule;
//        private StopLossRule _trailingStopLossRule;
//        private LotSizingRule _lotSizingRule;
//        private int _initialStopLossInPips;
//        private int _takeProfitInPips;
//        private bool _canOpenPosition;
//        private DateTime _lastClosedPositionTime;
//        private double _takeProfitLevel;
//        private double _recentHigh;
//        private bool _inpMoveToBreakEven;
//        private bool _alreadyMovedToBreakEven;

//        protected abstract string Name { get; }

//        protected abstract bool HasBullishSignal();
//        protected abstract bool HasBearishSignal();

//        protected void Init(
//            bool takeLongsParameter, 
//            bool takeShortsParameter, 
//            string initialStopLossRule,
//            string trailingStopLossRule,
//            string lotSizingRule,
//            int initialStopLossInPips = 0,
//            int takeProfitInPips = 0)
//        {
//            _takeLongsParameter = takeLongsParameter;
//            _takeShortsParameter = takeShortsParameter;
//            _initialStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), initialStopLossRule);
//            _trailingStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), trailingStopLossRule);
//            _lotSizingRule = (LotSizingRule)Enum.Parse(typeof(LotSizingRule), lotSizingRule);
//            _initialStopLossInPips = initialStopLossInPips;
//            _takeProfitInPips = takeProfitInPips;

//            _canOpenPosition = true;

//            Positions.Opened += OnPositionOpened;
//            Positions.Closed += OnPositionClosed;

//            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
//                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
//        }

//        //protected override void OnTick()
//        //{
//        //    var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
//        //    //var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

//        //    if (longPosition == null)
//        //    {
//        //        return;
//        //    }

//        //    ManageLongPosition(longPosition);
//        //}

//    protected override void OnBar()
//        {
//            //if (Positions.)


//            if (!_canOpenPosition)
//            {
//                return;
//            }

//            if (PendingOrders.Count > 0)
//            {
//                return;
//            }

//            // Wait for a little while after we exited a trade
//            if (_lastClosedPositionTime != DateTime.MinValue && Server.Time.Subtract(_lastClosedPositionTime).TotalMinutes <= 120)
//            {
//                Print("Pausing before we look for new opportunities.");
//                return;
//            }

//            double? stopLossLevel;
//            if (_takeLongsParameter && HasBullishSignal())
//            {
//                var Quantity = 1;

//                var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);               
//                stopLossLevel = 30;

//                if (stopLossLevel.HasValue)
//                {
//                    var targetPrice = MarketSeries.High.Maximum(2);

//                    // Take profit at 1:1 risk
//                    _takeProfitLevel = MarketSeries.Close.LastValue + stopLossLevel.Value * Symbol.PipSize;

//                    ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossLevel, null, null, null, true, StopTriggerMethod.Trade);
//                }
//            }
//            else if (_takeShortsParameter && HasBearishSignal())
//            {
//                var Quantity = 1;

//                var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
//                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, _initialStopLossInPips, _takeProfitInPips);
//            }
//        }

//        private void OnPositionOpened(PositionOpenedEventArgs args)
//        {
//            _currentPosition = args.Position;
//            var position = args.Position;
//            var sl = position.StopLoss.HasValue
//                ? string.Format(" (SL={0})", position.StopLoss.Value)
//                : string.Empty;

//            var tp = position.TakeProfit.HasValue
//                ? string.Format(" (TP={0})", position.TakeProfit.Value)
//                : string.Empty;

//            Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);
//            _canOpenPosition = false;
//        }

//        private void OnPositionClosed(PositionClosedEventArgs args)
//        {
//            var position = args.Position;
//            Print("Closed {0:N} {1} at {2} for {3} profit", position.VolumeInUnits, position.TradeType, position.EntryPrice, position.GrossProfit);

//            _lastClosedPositionTime = Server.Time;

//            _canOpenPosition = true;
//        }

//        private double? CalculateStopLossLevelForBuyOrder()
//        {
//            double? stopLossLevel = null;

//            switch (_initialStopLossRule)
//            {
//                case StopLossRule.None:
//                    break;

//                case StopLossRule.StaticPipsValue:
//                    stopLossLevel = _initialStopLossInPips;
//                    break;

//                case StopLossRule.CurrentBarNPips:
//                    stopLossLevel = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
//                    break;

//                case StopLossRule.PreviousBarNPips:
//                    var low = MarketSeries.Low.Last(1);
//                    if (MarketSeries.Low.Last(2) < low)
//                    {
//                        low = MarketSeries.Low.Last(2);
//                    }

//                    stopLossLevel = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
//                    break;
//            }

//            return stopLossLevel.HasValue
//                ? (double?)Math.Round(stopLossLevel.Value, Symbol.Digits)
//                : null;
//        }
//    }
//}
