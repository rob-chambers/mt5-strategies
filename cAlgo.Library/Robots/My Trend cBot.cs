using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;

namespace cAlgo
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

    //--- constants of identification of trend
    public enum TrendType
    {
        Flat,       // no trend
        HardDown,   // strong down trend
        Down,       // down trend
        SoftDown,   // weak down trend        
        SoftUp,     // weak up trend
        Up,         // up trend
        HardUp      // strong up trend
    };
    
    public abstract class BaseRobot : Robot
    {
        private int _digits_adjust;
        private double _adjustedPoints;
        private double _recentHigh;
        private double _recentSwingValue;
        //private bool _hadRecentSwing;
        private bool _alreadyMovedToBreakEven;

        protected StopLossRule InitialStopLossRuleValue { get; private set; }

        protected LotSizingRule LotSizingRuleValue { get; private set; }

        protected StopLossRule TrailingStopLossRuleValue { get; private set; }

        public string InitialStopLossRule { get; set; }

        public int InitialSLPips { get; set; }

        public string LotSizingRule { get; set; }

        public string TrailingStopLossRule { get; set; }

        public int TrailingSLPips { get; set; }

        public bool MoveToBreakEven { get; set; }

        public bool TakeLongs { get; set; }

        public bool TakeShorts { get; set; }

        protected abstract string Name { get; }

        protected virtual void NewBarAndNoCurrentPositions()
        {
        }

        protected void Init(
            string initialStopLossRule,
            int initialSLPips,
            string lotSizingRule,
            string trailingStopLossRule,
            int trailingSLPips,
            bool moveToBreakEven,
            bool takeLongs,
            bool takeShorts)
        {
            InitialStopLossRule = initialStopLossRule;
            InitialSLPips = initialSLPips;
            LotSizingRule = lotSizingRule;
            TrailingStopLossRule = trailingStopLossRule;
            TrailingSLPips = trailingSLPips;
            MoveToBreakEven = moveToBreakEven;
            TakeLongs = takeLongs;
            TakeShorts = takeShorts;
        }

        protected override void OnStart()
        {
            Print("Starting");
            ValidateParameters();
            InitDigitsAdjust();
            _adjustedPoints = Symbol.TickSize * _digits_adjust;
            _alreadyMovedToBreakEven = false;
        }

        protected override void OnError(Error error)
        {
            Print("There has been an Error: {0}", error.Code);
        }

        private void ValidateParameters()
        {
            const string StopLossRuleMessage = @"{0} Stop Loss Rule must be one of 'None', 'StaticPipsValue', 'CurrentBar2ATR', 'CurrentBarNPips', 'PreviousBarNPips', or 'ShortTermHighLow'.";

            Print("Validating parameters");
            StopLossRule enumValue;
            if (!Enum.TryParse(InitialStopLossRule, true, out enumValue))
            {
                StopWithError(string.Format(StopLossRuleMessage, "Initial "));
            }

            InitialStopLossRuleValue = enumValue;

            if (!Enum.TryParse(TrailingStopLossRule, true, out enumValue))
            {
                StopWithError(string.Format(StopLossRuleMessage, "Trailing "));
            }

            LotSizingRule lotSizingRule;
            if (!Enum.TryParse(LotSizingRule, true, out lotSizingRule))
            {
                StopWithError("Lot sizing rule must be either Static or Dynamic.");
            }

            TrailingStopLossRuleValue = enumValue;
            LotSizingRuleValue = lotSizingRule;


            //if (LotSizingRuleValue == LOTSIZING_RULE.Dynamic && inpLots != 0)
            //{
            //    Print("Invalid lot size - must be 0 when using dynamic sizing - init failed.");
            //    return (INIT_FAILED);
            //}

            //if ((inpLotSizingRule == Static) && !(inpLots > 0 && inpLots <= 10))
            //{
            //    Print("Invalid lot size - init failed.");
            //    return (INIT_FAILED);
            //}

            ValidateInitialStopLoss();
            ValidateTrailingStopLoss();

            //if (inpUseTakeProfit && inpTakeProfitPips <= 0 && inpTakeProfitRiskRewardRatio <= 0)
            //{
            //    Print("Invalid take profit pip value / risk reward ratio.  Pips or risk reward ratio should be greater than 0 - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (inpUseTakeProfit && inpTakeProfitPips > 0 && inpTakeProfitRiskRewardRatio > 0)
            //{
            //    Print("Invalid take profit parameters.  Can use only one of either take profit pip value or risk reward ratio - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (!inpUseTakeProfit && inpTakeProfitPips != 0)
            //{
            //    Print("Invalid take profit pip value.  Pips should be 0 when not using take profit - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (!inpUseTakeProfit && inpTakeProfitRiskRewardRatio != 0)
            //{
            //    Print("Invalid risk/reward ratio value.  Value should be 0 when not using take profit - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (inpMinutesToWaitAfterPositionClosed < 0)
            //{
            //    Print("Invalid number of minutes to wait after position closed. Value should be >= 0 - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (inpMinTradingHour < 0 || inpMinTradingHour > 23)
            //{
            //    Print("Invalid min trading hour. Value should be between 0 and 23 - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (inpMaxTradingHour < 0 || inpMaxTradingHour > 23)
            //{
            //    Print("Invalid max trading hour. Value should be between 0 and 23 - init failed.");
            //    return (INIT_FAILED);
            //}

            //if (inpMaxTradingHour < inpMinTradingHour)
            //{
            //    Print("Invalid min/max trading hours. Min should be less than or equal to max - init failed.");
            //    return (INIT_FAILED);
            //}

            if (InitialStopLossRuleValue == StopLossRule.None && TrailingStopLossRuleValue == StopLossRule.None)
            {
                StopWithError("Invalid stop loss rules - both initial and trailing are set to None - init failed.");
            }
        }

        private void ValidateInitialStopLoss()
        {
            switch (InitialStopLossRuleValue)
            {
                case StopLossRule.StaticPipsValue:
                    // Fall-through
                case StopLossRule.CurrentBarNPips:
                    // Fall-through
                case StopLossRule.PreviousBarNPips:
                    if (InitialSLPips <= 0)
                    {
                        StopWithError("Invalid initial stop loss pip value.  Pips should be greater than 0 - init failed.");
                    }
                    break;

                default:
                    if (InitialSLPips != 0)
                    {
                        StopWithError("Invalid initial stop loss rule.  Pips should be 0 when not using StaticPipsValue or CurrentBarNPips - init failed.");
                    }                    
                    break;
            }
        }

        private void ValidateTrailingStopLoss()
        {
            switch (TrailingStopLossRuleValue)
            {
                case StopLossRule.None:
                    // Fall-through
                case StopLossRule.ShortTermHighLow:
                    if (TrailingSLPips != 0)
                    {
                        StopWithError("Invalid trailing stop loss rule.  Pips should be 0 when not using StaticPipsValue - init failed.");
                    }

                    break;

                default:
                    if (TrailingSLPips <= 0)
                    {
                        StopWithError("Invalid trailing stop loss pip value.  Pips should be greater than 0 - init failed.");
                    }

                    break;
            }
        }

        protected void StopWithError(string errorMessage)
        {
            Stop();
            throw new Exception(errorMessage);
        }

        private void InitDigitsAdjust()
        {
            _digits_adjust = 1;
            if (Symbol.Digits == 5 || Symbol.Digits == 3 || Symbol.Digits == 1)
            {
                _digits_adjust = 10;
            }
        }

        public virtual void Deinit()
        {
        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

            if (longPosition != null || shortPosition != null)
            {
                CheckToModifyPositions();
                return;
            }
        }

        protected override void OnBar()
        {
            var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

            if (longPosition != null || shortPosition != null)
            {
                return;
            }

            //if (IsOutsideTradingHours())
            //{
            //    return;
            //}

            //if (ShouldPauseUntilOpeningNewPosition())
            //{
            //    return;
            //}

            double limitPrice;
            double lotSize = 1;
            double stopLossLevel = 0;

            NewBarAndNoCurrentPositions();

            if (TakeLongs && HasBullishSignal())
            {
                limitPrice = Symbol.Ask;
                stopLossLevel = CalculateStopLossLevelForBuyOrder();

                //if (_inpUseTakeProfit)
                //{
                //    if (_inpTakeProfitRiskRewardRatio > 0)
                //    {
                //        double distance = (limitPrice - stopLossLevel) * _inpTakeProfitRiskRewardRatio;
                //        takeProfitLevel = limitPrice + distance;
                //    }
                //    else
                //    {
                //        takeProfitLevel = limitPrice + takeProfitPipsFinal * _Point * _digits_adjust;
                //    }
                //}
                //else
                //{
                //    takeProfitLevel = 0.0;
                //}

                if (LotSizingRuleValue == cAlgo.LotSizingRule.Dynamic)
                {
                    // TODO: lotSize = _fixedRisk.CheckOpenLong(limitPrice, stopLossLevel);
                    lotSize = 1;
                }

                //OpenPosition(_Symbol, ORDER_TYPE_BUY, lotSize, limitPrice, stopLossLevel, takeProfitLevel);
                var volumeInUnits = Symbol.QuantityToVolume(lotSize);

                Print("Buying {0} units ({1} lots) at market with SL of {2}", volumeInUnits, lotSize, stopLossLevel);
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossLevel, null);
            }
            else if (TakeShorts && HasBearishSignal())
            {
                limitPrice = Symbol.Bid;
                //stopLossLevel = CalculateStopLossLevelForSellOrder();

                //if (_inpUseTakeProfit)
                //{
                //    if (_inpTakeProfitRiskRewardRatio > 0)
                //    {
                //        double distance = (stopLossLevel - limitPrice) * _inpTakeProfitRiskRewardRatio;
                //        takeProfitLevel = limitPrice - distance;
                //    }
                //    else
                //    {
                //        takeProfitLevel = limitPrice - takeProfitPipsFinal * _Point * _digits_adjust;
                //    }
                //}
                //else
                //{
                //    takeProfitLevel = 0.0;
                //}

                if (LotSizingRuleValue == cAlgo.LotSizingRule.Dynamic)
                {
                    //lotSize = _fixedRisk.CheckOpenShort(limitPrice, stopLossLevel);
                    lotSize = 1;
                }

                //OpenPosition(_Symbol, ORDER_TYPE_SELL, lotSize, limitPrice, stopLossLevel, takeProfitLevel);
                var volumeInUnits = Symbol.QuantityToVolume(lotSize);
                Print("Selling {0} units ({1} lots) at market with SL of {2}", volumeInUnits, lotSize, stopLossLevel);
                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossLevel, null);
            }
        }

        public virtual void OnTrade()
        {
        }

        public virtual bool HasBullishSignal()
        {
            return false;
        }

        public virtual bool HasBearishSignal()
        {
            return false;
        }

        private bool CheckToModifyPositions()
        {
            if (TrailingStopLossRuleValue == StopLossRule.None && !MoveToBreakEven) return false;

            var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
            if (longPosition != null)
            {
                if (LongModified(longPosition))
                    return true;
            }
            
            //var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

            //if (_position.PositionType() == POSITION_TYPE_BUY)
            //{
            //    if (LongModified())
            //        return true;
            //}
            //else
            //{
            //    if (ShortModified())
            //        return true;
            //}

            return false;
        }

        private bool LongModified(Position position)
        {
            Print("Checking to modify long position");

            double newStop = 0;

            //if (_barsSincePositionOpened == 0) {
            //    return false;
            //}

            // Are we making higher highs?

            double currentHigh = MarketSeries.High[1];
            double priorHigh = MarketSeries.High[2];
            double currentLow = MarketSeries.Low[1];
            double priorLow = MarketSeries.Low[2];

            if (currentHigh > priorHigh && currentHigh > _recentHigh)
            {
                _recentHigh = currentHigh;
                _recentSwingValue = currentLow;
                //_hadRecentSwing = false;
            }
            else
            {
                return false;
            }
            
            double initialRisk = position.EntryPrice - position.StopLoss.GetValueOrDefault();
            double breakEvenPoint = position.EntryPrice + initialRisk;

            switch (TrailingStopLossRuleValue)
            {
                case StopLossRule.StaticPipsValue:
                    newStop = _recentHigh - TrailingSLPips;
                    break;

                case StopLossRule.CurrentBarNPips:
                    newStop = currentLow - _adjustedPoints * TrailingSLPips;
                    break;

                //case CurrentBar2ATR:
                //    newStop = _currentAsk - _atrData[0] * 2;
                //    break;

                case StopLossRule.PreviousBarNPips:
                    var barLow = Math.Min(priorLow, currentLow);
                    newStop = barLow - _adjustedPoints * TrailingSLPips;
                    break;
            }

            // TOOD: Is the new stop sufficiently far away or perhaps too far?

            // Check if we should move to breakeven
            if (MoveToBreakEven && !_alreadyMovedToBreakEven)
            {
                if (Symbol.Ask > breakEvenPoint)
                {
                    if (newStop == 0.0 || breakEvenPoint > newStop)
                    {
                        Print("Moving to breakeven now that the price has reached {0}", breakEvenPoint);
                        newStop = position.EntryPrice;
                    }
                }
            }

            if (newStop == 0.0)
            {
                return false;
            }

            double sl = Math.Round(newStop, Symbol.Digits);
            //double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

            if (position.StopLoss.GetValueOrDefault() > sl)
            {
                return false;
            }

            //double diff = (Symbol.Ask - sl) / _adjustedPoints;
            //if (diff < stopLevelPips)
            //{
            //    printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
            //        _currentAsk, sl, stopLevelPips, diff);

            //    sl = _currentAsk - stopLevelPips * _adjustedPoints;
            //}

            //--- modify position
            var result = ModifyPosition(position, sl, position.TakeProfit);
            if (!result.IsSuccessful)
            {
                Print("Error modifying position for {0} : {1}", Symbol.Code, result.Error);
                Print("Modify parameters : SL={0},TP={1}", sl, position.TakeProfit);
            }

            if (!_alreadyMovedToBreakEven && sl >= position.EntryPrice)
            {
                var profitInPips = Convert.ToInt32((sl - position.EntryPrice) / _adjustedPoints);
                Print("{0} pips profit now locked in (sl = {1}, open = {2})", profitInPips, sl, position.EntryPrice);
                _alreadyMovedToBreakEven = true;
            }

            return true;
        }

        double CalculateStopLossLevelForBuyOrder()
        {
            double stopLossPipsFinal = 0;
            double stopLossLevel = 0;
            double stopLevelPips = 0;
            double low;
            double priceFromStop;
            double currentLow = MarketSeries.Low[1];
            double priorLow = MarketSeries.Low[2];
            double point = Symbol.TickSize;

            //stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

            Print("Calculating SL for buy order");

            switch (InitialStopLossRuleValue)
            {
                case StopLossRule.None:
                    stopLossLevel = 0;
                    break;

                case StopLossRule.StaticPipsValue:
                    if (InitialSLPips < stopLevelPips)
                    {
                        stopLossPipsFinal = stopLevelPips;
                    }
                    else
                    {
                        stopLossPipsFinal = InitialSLPips;
                    }

                    //stopLossLevel = Symbol.Ask - stopLossPipsFinal * point * _digits_adjust;
                    break;

                //case STOPLOSS_RULE.CurrentBarNPips:
                //    pips = InitialSLPips;
                //    stopLossLevel = currentLow - _adjustedPoints * pips;
                //    priceFromStop = (Symbol.Ask - stopLossLevel) / (point * _digits_adjust);

                //    if (priceFromStop < stopLevelPips)
                //    {
                //        Print("calculated stop too close to price.  adjusting from {0} to {1}", priceFromStop, stopLevelPips);
                //        stopLossPipsFinal = stopLevelPips;
                //    }
                //    else
                //    {
                //        stopLossPipsFinal = priceFromStop;
                //    }

                //    //stopLossLevel = Symbol.Ask - stopLossPipsFinal * point * _digits_adjust;
                //    break;

                //case STOPLOSS_RULE.CurrentBar2ATR:
                //    stopLossLevel = Symbol.Ask - _atrData[0] * 2;
                //    break;

                case StopLossRule.CurrentBarNPips:
                    // fall-through
                case StopLossRule.PreviousBarNPips:
                    if (InitialStopLossRuleValue == StopLossRule.CurrentBarNPips)
                    {
                        low = currentLow;
                    }
                    else
                    {
                        low = Math.Min(currentLow, priorLow);
                    }
                    
                    stopLossLevel = low - _adjustedPoints * InitialSLPips;
                    priceFromStop = (Symbol.Ask - stopLossLevel) / (point * _digits_adjust);

                    if (priceFromStop < stopLevelPips)
                    {
                        Print("calculated stop too close to price.  adjusting from {0} to {1}", priceFromStop, stopLevelPips);
                        stopLossPipsFinal = stopLevelPips;
                    }
                    else
                    {
                        stopLossPipsFinal = priceFromStop;
                    }
                    break;
            }

            //double sl = NormalizeDouble(stopLossLevel, _symbol.Digits());
            //var sl = Math.Round(stopLossLevel, Symbol.Digits);
            //return sl;
            return stopLossPipsFinal;
        }
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MyTrendcBot : BaseRobot
    {
        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Initial SL rule", DefaultValue = StopLossRule.CurrentBarNPips)]
        public string InitialStopLossRuleParameter { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 0, MinValue = 0, Step = 1)]
        public int InitialSLPipsParameter { get; set; }

        [Parameter("Lot sizing rule", DefaultValue = cAlgo.LotSizingRule.Dynamic)]
        public string LotSizingRuleParameter { get; set; }

        [Parameter("Trailing SL rule", DefaultValue = StopLossRule.ShortTermHighLow)]
        public string TrailingStopLossRuleParameter { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 0, MinValue = 0, Step = 1)]
        public int TrailingSLPipsParameter { get; set; }

        [Parameter("Move to breakeven?", DefaultValue = true)]
        public bool MoveToBreakEvenParameter { get; set; }

        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 70)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 25)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Short term rejection multiplier", DefaultValue = 1.5, MinValue = 0.5, MaxValue = 4)]
        public double ShortTermTrendRejectionMultiplier { get; set; }

        [Parameter("Strong trend threshold", DefaultValue = 2, MinValue = 1, Step = 0.1)]
        public double StrongTrendThreshold { get; set; }

        [Parameter("Medium trend threshold", DefaultValue = 0.4, MinValue = 0.1, Step = 0.1)]
        public double MediumTrendThreshold { get; set; }

        [Parameter("Weak trend threshold", DefaultValue = 0.2, MinValue = 0.05, Step = 0.05)]
        public double WeakTrendThreshold { get; set; }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private AverageTrueRange _atr;

        protected override void OnStart()
        {
            Init(InitialStopLossRuleParameter, InitialSLPipsParameter, LotSizingRuleParameter, TrailingStopLossRuleParameter, TrailingSLPipsParameter, MoveToBreakEvenParameter, TakeLongsParameter, TakeShortsParameter);
            base.OnStart();
            _fastMA = Indicators.MovingAverage(SourceSeries, 50, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, 100, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, 240, MovingAverageType.Weighted);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            ValidateThresholds();

            double currentHigh = MarketSeries.High[1];
            double currentClose = MarketSeries.Close[1];
            double currentLow = MarketSeries.Low[1];

            Print("HLC: {0},{1},{2}", currentHigh, currentLow, currentClose);
        }

        private void ValidateThresholds()
        {
            Print("Validating thresholds");
            if (!(StrongTrendThreshold > MediumTrendThreshold && MediumTrendThreshold > WeakTrendThreshold))
            {
                StopWithError("Invalid value(s) for trend thresholds");
            }
        }

        protected override string Name { get { return "My Trend cBot"; } }

        public override bool HasBullishSignal()
        {
            /*
    Rule 1 - We must have closed near the high (i.e. had an up bar)
    Rule 2 - The slope of the long-term trend must be up
    Rule 3 - The current low must be higher than long-term MA (or maybe within 6 pips?)
    Rule 4 - The current low must be less than or equal to the short-term MA or within a pip or two (or perhaps at least one recent bar's low was lower)
    Rule 5 - The current close is higher than the short-term MA
    Rule 6 - The slope of the short-term MA must be flat or rising
    Rule 7 - There hasn't been a recent (in the last 15 or 20 bars) highest high that is more than a certain level above the current close (using ATR)
*/

            /*
            Second case scenario...

            Special very bullish scenario (not based on QMP filter signal)
            Low < all 3 15M MAs
            High > all 3 15M MAs
            Slope of short-term 15M MA is flat or rising
            */

            /*
            TREND_TYPE values:

            TREND_TYPE_HARD_DOWN = 0,   // strong down trend
            TREND_TYPE_DOWN = 1,        // down trend
            TREND_TYPE_SOFT_DOWN = 2,   // weak down trend
            TREND_TYPE_FLAT = 3,        // no trend
            TREND_TYPE_SOFT_UP = 4,     // weak up trend
            TREND_TYPE_UP = 5,          // up trend
            TREND_TYPE_HARD_UP = 6      // strong up trend
            */

            //int currBar = MarketSeries.Close.Count - 1;
            //Print("Current bar: {0}", currBar);

            double currentHigh = MarketSeries.High[1];
            double currentClose = MarketSeries.Close[1];
            double currentLow = MarketSeries.Low[1];

            if (currentHigh - currentClose > currentClose - currentLow) return false;

            Print("Found bullish bar - HLC = {0}, {1}, {2}", currentHigh, currentLow, currentClose);

            // Special case first
            if (currentLow < _fastMA.Result.LastValue &&
                currentLow < _slowMA.Result.LastValue &&
                currentLow < _mediumMA.Result.LastValue &&
                currentHigh > _fastMA.Result.LastValue &&
                currentHigh > _mediumMA.Result.LastValue &&
                currentHigh > _slowMA.Result.LastValue)
            {
                //TrendType longTrend = LongTermTrend();
                //Print("For special case, Long term trend determined to be: ", longTrend);
                //switch (longTrend)
                //{
                //    case TREND_TYPE_UP:
                //    // Fall-through
                //    case TREND_TYPE_HARD_UP:
                //    // Fall-through
                //    case TREND_TYPE_SOFT_UP:
                //        break;

                //    default:
                //        return false;
                //}

                Print("Special case bar found!");

                return true;
            }

            return false;

            //if (_prices[1].low < _longTermTrendData[1]) return false;
            //if (_prices[1].low > _shortTermTrendData[1]) return false;
            //if (_prices[1].close <= _shortTermTrendData[1]) return false;

            //TrendType longTrend = LongTermTrend();
            //Print("Long term trend determined to be: ", GetTrendDescription(longTrend));
            //switch (longTrend)
            //{
            //    case TREND_TYPE_UP:
            //    // Fall-through
            //    case TREND_TYPE_HARD_UP:
            //        break;

            //    default:
            //        return false;
            //}

            //Print("Checking short-term trend");
            //TrendType shortTrend = ShortTermTrend();
            //Print("Short term trend determined to be: ", GetTrendDescription(shortTrend));
            //switch (shortTrend)
            //{
            //    case TREND_TYPE_FLAT:
            //    // Fall-through
            //    case TREND_TYPE_SOFT_UP:
            //    // Fall-through
            //    case TREND_TYPE_UP:
            //    // Fall-through
            //    case TREND_TYPE_HARD_UP:
            //        break;

            //    default:
            //        return false;
            //}

            //if (HadRecentHigh())
            //{
            //    return false;
            //}

            //return true;

        }

        //private TrendType LongTermTrend()
        //{            
        //    double recentATR = _atr.Result.LastValue;
        //    double priorATR = _atr.Result.

        //    TrendType trend = Trend(_longTermTimeFrameData[1], _longTermTimeFrameData[_inpLongTermPeriod - 1], recentATR, priorATR);
        //    return trend;
        //}

        //TrendType ShortTermTrend()
        //{
        //    double recentATR = _shortTermATRData[0];
        //    double priorATR = _shortTermATRData[_inpShortTermPeriod - 1];

        //    TrendType trend = Trend(_shortTermTrendData[1], _shortTermTrendData[_inpShortTermPeriod - 1], recentATR, priorATR);
        //    return trend;
        //}

        //TrendType Trend(double recentValue, double priorValue, double recentATR, double priorATR)
        //{
        //    double diff = recentValue - priorValue;
        //    double atr = (recentATR + priorATR) / 2;
        //    TrendType trend = TrendType.Flat;

        //    double ratio = diff / atr;

        //    Print("Recent={0}, Prior={1}, Diff={2}, recentATR={3}, priorATR={4}, ratio={5}", recentValue, priorValue, diff, recentATR, priorATR, ratio);

        //    if (ratio >= StrongTrendThreshold)
        //    {
        //        trend = TrendType.HardUp;
        //    }
        //    else if (ratio >= MediumTrendThreshold)
        //    {
        //        trend = TrendType.Up;
        //    }
        //    else if (ratio >= WeakTrendThreshold)
        //    {
        //        trend = TrendType.SoftUp;
        //    }
        //    else if (ratio <= -WeakTrendThreshold)
        //    {
        //        trend = TrendType.SoftDown;
        //    }
        //    else if (ratio <= -MediumTrendThreshold)
        //    {
        //        trend = TrendType.Down;
        //    }
        //    else if (ratio <= -StrongTrendThreshold)
        //    {
        //        trend = TrendType.HardDown;
        //    }

        //    return trend;
        //}


        public override bool HasBearishSignal()
        {
            return false;
        }
    }
}
