// Version 2020-04-11 14:15
using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;

namespace cAlgo.Library.Robots.MAAdvancedCrosser
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class MAAdvancedCrosserBot : BaseRobot
    {
        #region Standard Parameters
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = 4)]
        public InitialStopLossRuleValues InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = 0)]
        public TrailingStopLossRuleValues TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = 0)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Take Profit Rule", DefaultValue = 2)]
        public TakeProfitRuleValues TakeProfitRule { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 0)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Pause after position closed (Minutes)", DefaultValue = 0)]
        public int MinutesToWaitAfterPositionClosed { get; set; }

        [Parameter("Move to breakeven?", DefaultValue = false)]
        public bool MoveToBreakEven { get; set; }

        [Parameter("Close half at breakeven?", DefaultValue = false)]
        public bool CloseHalfAtBreakEven { get; set; }

        [Parameter("Dynamic Risk Percentage?", DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Bars for trade development", DefaultValue = 0)]
        public int BarsToAllowTradeToDevelop { get; set; }

        #endregion

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = 2)]
        public MaCrossRuleValues MaCrossRule { get; set; }

        [Parameter("Apply MA Distance filter", DefaultValue = false)]
        public bool MADistanceFilter { get; set; }

        [Parameter("Apply MA Max Distance filter", DefaultValue = false)]
        public bool MAMaxDistanceFilter { get; set; }

        [Parameter("Apply Flat MAs filter", DefaultValue = false)]
        public bool MAsFlatFilter { get; set; }

        [Parameter("New high/low filter", DefaultValue = false)]
        public bool NewHighLowFilter { get; set; }

        protected override string Name
        {
            get
            {
                return "MAAdvancedCrosser";
            }
        }

        private MACrossOver _maCrossIndicator;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;        
        private bool _enterAtMarket;
        private double _entryPrice;
        private TradeResult _currentTradeResult;

        protected override void OnStart()
        {
            _maCrossIndicator = Indicators.GetIndicator<MACrossOver>(SourceSeries, SlowPeriodParameter, MediumPeriodParameter, FastPeriodParameter);
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Initial SL in pips: {0}", InitialStopLossInPips);
            Print("Trailing SL rule: {0}", TrailingStopLossRule);
            Print("Trailing SL in pips: {0}", TrailingStopLossInPips);
            Print("Lot sizing rule: {0}", LotSizingRule);
            Print("Take profit rule: {0}", TakeProfitRule);
            Print("Take profit in pips: {0}", TakeProfitInPips);
            Print("Minutes to wait after position closed: {0}", MinutesToWaitAfterPositionClosed);
            Print("Move to breakeven: {0}", MoveToBreakEven);
            Print("Close half at breakeven: {0}", CloseHalfAtBreakEven);
            Print("MA Cross Rule: {0}", MaCrossRule);
            Print("MADistanceFilter: {0}", MADistanceFilter);
            Print("MAsFlatFilter: {0}", MAsFlatFilter);
            Print("NewHighLowFilter: {0}", NewHighLowFilter);
            Print("BarsToAllowTradeToDevelop: {0}", BarsToAllowTradeToDevelop);            

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
                TakeProfitRule,
                TakeProfitInPips,                
                MinutesToWaitAfterPositionClosed,
                MoveToBreakEven,
                CloseHalfAtBreakEven,
                DynamicRiskPercentage,
                BarsToAllowTradeToDevelop,
                MaCrossRule);

            Notifications.SendEmail("rechambers11@gmail.com", "rechambers11@gmail.com", "MA Cross Over robot initialized", "This is a test");
        }

        protected override void ValidateParameters(
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
            int barsToAllowTradeToDevelop,
            MaCrossRuleValues maCrossRule)
        {
            base.ValidateParameters(
                takeLongsParameter, 
                takeShortsParameter, 
                initialStopLossRule, 
                initialStopLossInPips, 
                trailingStopLossRule, 
                trailingStopLossInPips, 
                lotSizingRule,
                takeProfitRule,
                takeProfitInPips, 
                minutesToWaitAfterPositionClosed, 
                moveToBreakEven, 
                closeHalfAtBreakEven,
                dynamicRiskPercentage,
                barsToAllowTradeToDevelop,
                maCrossRule);

            if (FastPeriodParameter <= 0 || FastPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Fast MA Period' - must be between 1 and 999");

            if (MediumPeriodParameter <= 0 || MediumPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Medium MA Period' - must be between 1 and 999");

            if (SlowPeriodParameter <= 0 || SlowPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Slow MA Period' - must be between 1 and 999");

            if (!(FastPeriodParameter < MediumPeriodParameter && MediumPeriodParameter < SlowPeriodParameter))
                throw new ArgumentException("Invalid 'MA Periods' - fast must be less than medium and medium must be less than slow");

            var initialSLRule = (InitialStopLossRuleValues)initialStopLossRule;
            var trailingSLRule = (TrailingStopLossRuleValues)trailingStopLossRule;

            if (_maCrossRule == MaCrossRuleValues.None && initialSLRule == InitialStopLossRuleValues.None && trailingSLRule == TrailingStopLossRuleValues.None)
                throw new ArgumentException("The combination of parameters means that a position may incur a massive loss");
        }

        protected override void EnterLongPosition()
        {
            if (_enterAtMarket)
            {
                base.EnterLongPosition();
                return;
            }

            //var stopLossPips = CalculateInitialStopLossInPipsForLongPosition();
            //double lots;

            //if (stopLossPips.HasValue)
            //{
            //    lots = CalculatePositionQuantityInLots(stopLossPips.Value);
            //    Print("SL calculated for Buy order = {0}", stopLossPips);
            //}
            //else
            //{
            //    lots = 1;
            //    Print("Warning - no SL set.  Setting lot quantity to {0}.", lots);
            //}

            //var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
        
            //var expiry = Server.Time.AddDays(1);

            //Print("Placing BUY STOP order at {0} with stop loss {1}", _entryPrice, stopLossPips);
            //var label = string.Format("BUY {0}", Symbol);
            //_currentTradeResult = PlaceStopOrder(TradeType.Buy, Symbol.Name, volumeInUnits, _entryPrice, label, stopLossPips, CalculateTakeProfit(stopLossPips), expiry);
        }

        protected override void EnterShortPosition()
        {            
            if (_enterAtMarket)
            {
                base.EnterShortPosition();
                return;
            }

            //var label = string.Format("SELL {0}", Symbol);
            //Print("Placing limit order at {0} with stop {1}", limitPrice, stopLossPips);
            //_currentTradeResult = PlaceLimitOrder(TradeType.Sell, Symbol.Name, volumeInUnits, limitPrice, label, stopLossPips, CalculateFibTakeProfit(), expiry);
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            if (_initialStopLossRule == InitialStopLossRuleValues.Custom)
            {
                double stop;
                if (Symbol.Ask < _fastMA.Result.LastValue)
                {
                    // A Buy stop order - price is actually less than the MA
                    Print("Calculating SL for Buy Stop. Entry price = {0} and fast MA = {1}", _entryPrice, _fastMA.Result.LastValue);

                    stop = (_entryPrice - _fastMA.Result.LastValue) / Symbol.PipSize;
                }
                else
                {
                    stop = (Symbol.Ask - _fastMA.Result.LastValue) / Symbol.PipSize;
                    if (stop < 5)
                    {
                        Print("Stop too tight with fast MA - trying medium MA");
                        stop = (Symbol.Ask - _mediumMA.Result.LastValue) / Symbol.PipSize;
                        if (stop < 5)
                        {
                            Print("Stop too tight with medium MA - trying slow MA");
                            stop = (Symbol.Ask - _slowMA.Result.LastValue) / Symbol.PipSize;
                        }
                    }
                }
                
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForLongPosition();
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            if (_initialStopLossRule == InitialStopLossRuleValues.Custom)
            {
                var stop = (_fastMA.Result.LastValue - Symbol.Bid) / Symbol.PipSize;
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForShortPosition();
        }

        protected override bool HasBullishSignal()
        {
            var value = _maCrossIndicator.UpSignal.Last(1);
            if (value.Equals(double.NaN))
                return false;

            // Determine whether we enter now at the market or use a Buy Stop to confirm the up move

            // Have we hit a new high?
            //const int HighestHighPeriod = 70;

            //var high = MarketSeries.High.Maximum(HighestHighPeriod);
            //var priorHigh = MarketSeries.High.Last(1);

            //Print("New signal - high = {0} and max = {1}", priorHigh, high);

            //if (priorHigh != high)
            //{
            //    Print("Market entry setup rejected as the prior high {0} has not gone higher than {1}", priorHigh, high);

            //    _enterAtMarket = false;

            //    // Enter at high plus a buffer
            //    _entryPrice = high + 2 * Symbol.PipSize;

            //    return false;
            //}
            //else
            //{
            //    _enterAtMarket = true;
            //}            

            _enterAtMarket = true;
            return true;
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {
            base.OnPositionOpened(args);
            ShouldTrail = false;
        }

        protected override bool HasBearishSignal()
        {
            var value = _maCrossIndicator.DownSignal.Last(1);
            if (!value.Equals(double.NaN))
            {
                return true;
            }

            return false;
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

            switch (_maCrossRule)
            {
                case MaCrossRuleValues.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case MaCrossRuleValues.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return true;
            }

            if (Bars.ClosePrices.Last(1) < value - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the {0} MA", maType);
                _canOpenPosition = true;
                _currentPosition.Close();
            }

            return true;
        }

        protected override bool ManageShortPosition()
        {
            if (!ShouldTrail && Symbol.Bid < TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop lower
            if (!base.ManageShortPosition())
                return false;

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case MaCrossRuleValues.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case MaCrossRuleValues.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return true;
            }

            if (Bars.ClosePrices.Last(1) > value + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the {0} MA", maType);
                _currentPosition.Close();
            }

            return true;
        }
    }

    //public abstract class BaseRobot : Robot
    //{
    //    protected const int InitialRecentLow = int.MaxValue;
    //    protected const int InitialRecentHigh = 0;

    //    protected abstract string Name { get; }
    //    protected Position _currentPosition;
    //    protected double ExitPrice { get; private set; }
    //    protected int BarsSinceEntry { get; private set; }
    //    protected double RecentLow { get; set; }
    //    protected double RecentHigh { get; set; }
    //    protected bool ShouldTrail { get; set; }
    //    protected double BreakEvenPrice { get; private set; }
    //    protected double DoubleRiskPrice { get; private set; }
    //    protected double TripleRiskPrice { get; private set; }
    //    protected double? TrailingInitiationPrice { get; private set; }

    //    private bool _takeLongsParameter;
    //    private bool _takeShortsParameter;
    //    protected bool _canOpenPosition;
    //    protected InitialStopLossRule _initialStopLossRule;
    //    private TrailingStopLossRule _trailingStopLossRule;
    //    private LotSizingRule _lotSizingRule;
    //    private int _initialStopLossInPips;
    //    private TakeProfitRule _takeProfitRule;
    //    private int _takeProfitInPips;
    //    private int _trailingStopLossInPips;
    //    private int _minutesToWaitAfterPositionClosed;
    //    private bool _moveToBreakEven;
    //    private bool _closeHalfAtBreakEven;
    //    private double _dynamicRiskPercentage;
    //    private int _barsToAllowTradeToDevelop;        
    //    private DateTime _lastClosedPositionTime;
    //    private bool _alreadyMovedToBreakEven;        
    //    private bool _isClosingHalf;        

    //    protected abstract bool HasBullishSignal();
    //    protected abstract bool HasBearishSignal();

    //    protected void Init(
    //        bool takeLongsParameter, 
    //        bool takeShortsParameter, 
    //        int initialStopLossRule,
    //        int initialStopLossInPips,
    //        int trailingStopLossRule,
    //        int trailingStopLossInPips,
    //        int lotSizingRule,         
    //        int takeProfitRule,
    //        int takeProfitInPips = 0,            
    //        int minutesToWaitAfterPositionClosed = 0,
    //        bool moveToBreakEven = false,
    //        bool closeHalfAtBreakEven = false,
    //        double dynamicRiskPercentage = 2,
    //        int barsToAllowTradeToDevelop = 0)
    //    {
    //        ValidateParameters(takeLongsParameter, takeShortsParameter, initialStopLossRule, initialStopLossInPips,
    //                trailingStopLossRule, trailingStopLossInPips, lotSizingRule, takeProfitRule, takeProfitInPips,
    //                minutesToWaitAfterPositionClosed, moveToBreakEven, closeHalfAtBreakEven, dynamicRiskPercentage, barsToAllowTradeToDevelop);

    //        _takeLongsParameter = takeLongsParameter;
    //        _takeShortsParameter = takeShortsParameter;
    //        _initialStopLossRule = (InitialStopLossRule)initialStopLossRule;
    //        _initialStopLossInPips = initialStopLossInPips;
    //        _trailingStopLossRule = (TrailingStopLossRule)trailingStopLossRule;
    //        _trailingStopLossInPips = trailingStopLossInPips;
    //        _lotSizingRule = (LotSizingRule)lotSizingRule;
    //        _takeProfitRule = (TakeProfitRule)takeProfitRule;
    //        _takeProfitInPips = takeProfitInPips;
    //        _minutesToWaitAfterPositionClosed = minutesToWaitAfterPositionClosed;
    //        _moveToBreakEven = moveToBreakEven;
    //        _closeHalfAtBreakEven = closeHalfAtBreakEven;
    //        _dynamicRiskPercentage = dynamicRiskPercentage;
    //        _barsToAllowTradeToDevelop = barsToAllowTradeToDevelop;

    //        _canOpenPosition = true;

    //        Positions.Opened += OnPositionOpened;
    //        Positions.Closed += OnPositionClosed;
    //        Positions.Modified += OnPositionModified;

    //        Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
    //            Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
    //    }

    //    protected virtual void ValidateParameters(
    //        bool takeLongsParameter, 
    //        bool takeShortsParameter, 
    //        int initialStopLossRule, 
    //        int initialStopLossInPips, 
    //        int trailingStopLossRule,
    //        int trailingStopLossInPips,
    //        int lotSizingRule,
    //        int takeProfitRule,
    //        int takeProfitInPips,
    //        int minutesToWaitAfterPositionClosed,
    //        bool moveToBreakEven,
    //        bool closeHalfAtBreakEven,
    //        double dynamicRiskPercentage,
    //        int barsToAllowTradeToDevelop)
    //    {
    //        if (!takeLongsParameter && !takeShortsParameter)
    //            throw new ArgumentException("Must take at least longs or shorts");

    //        if (!Enum.IsDefined(typeof(InitialStopLossRule), initialStopLossRule))
    //            throw new ArgumentException("Invalid initial stop loss rule");

    //        if (!Enum.IsDefined(typeof(TrailingStopLossRule), trailingStopLossRule))
    //            throw new ArgumentException("Invalid trailing stop loss rule");

    //        if (initialStopLossInPips < 0 || initialStopLossInPips > 999)
    //            throw new ArgumentException("Invalid initial stop loss - must be between 0 and 999");

    //        if (trailingStopLossInPips < 0 || trailingStopLossInPips > 999)
    //            throw new ArgumentException("Invalid trailing stop loss - must be between 0 and 999");

    //        if (!Enum.IsDefined(typeof(LotSizingRule), lotSizingRule))
    //            throw new ArgumentException("Invalid lot sizing rule");

    //        if (takeProfitInPips < 0 || takeProfitInPips > 999)
    //            throw new ArgumentException("Invalid take profit - must be between 0 and 999");

    //        if (!Enum.IsDefined(typeof(TakeProfitRule), takeProfitRule))
    //            throw new ArgumentException("Invalid take profit rule");

    //        if ((TakeProfitRule)takeProfitRule != TakeProfitRule.StaticPipsValue && takeProfitInPips != 0)
    //            throw new ArgumentException("Invalid take profit - must be 0 when Take Profit Rule is not Static Pips");

    //        if (minutesToWaitAfterPositionClosed < 0 || minutesToWaitAfterPositionClosed > 60 * 24)
    //            throw new ArgumentException(string.Format("Invalid 'Pause after position closed' - must be between 0 and {0}", 60 * 24));

    //        if (!moveToBreakEven && closeHalfAtBreakEven)
    //            throw new ArgumentException("'Close half at breakeven?' is only valid when 'Move to breakeven?' is set");

    //        var lotSizing = (LotSizingRule)lotSizingRule;
    //        if (lotSizing == LotSizingRule.Dynamic && (dynamicRiskPercentage <= 0 || dynamicRiskPercentage >= 10))
    //            throw new ArgumentOutOfRangeException("Dynamic Risk value is out of range - it is a percentage (e.g. 2)");

    //        if (barsToAllowTradeToDevelop < 0 || barsToAllowTradeToDevelop > 99)
    //            throw new ArgumentOutOfRangeException("BarsToAllowTradeToDevelop is out of range - must be between 0 and 99");
    //    }

    //    protected override void OnTick()
    //    {
    //        if (_currentPosition == null)
    //            return;

    //        ManageExistingPosition();                
    //    }

    //    protected override void OnBar()
    //    {
    //        if (_currentPosition != null)
    //        {
    //            BarsSinceEntry++;
    //            //Print("Bars since entry: {0}", BarsSinceEntry);
    //        }

    //        if (!_canOpenPosition || PendingOrders.Any())
    //            return;

    //        if (ShouldWaitBeforeLookingForNewSetup())
    //            return;

    //        if (_takeLongsParameter && HasBullishSignal())
    //        {
    //            EnterLongPosition();
    //        }
    //        else if (_takeShortsParameter && HasBearishSignal())
    //        {
    //            EnterShortPosition();
    //        }
    //    }

    //    private void ManageExistingPosition()
    //    {
    //        switch (_currentPosition.TradeType)
    //        {
    //            case TradeType.Buy:
    //                ManageLongPosition();
    //                break;

    //            case TradeType.Sell:
    //                ManageShortPosition();
    //                break;
    //        }
    //    }

    //    /// <summary>
    //    /// Manages an existing long position.  Note this method is called on every tick.
    //    /// </summary>
    //    protected virtual bool ManageLongPosition()
    //    {
    //        if (BarsSinceEntry <= _barsToAllowTradeToDevelop)
    //            return false;

    //        if (_trailingStopLossRule == TrailingStopLossRule.None && !_moveToBreakEven)
    //            return true;

    //        // Are we making higher highs?
    //        var madeNewHigh = false;

    //        if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Ask >= BreakEvenPrice)
    //        {
    //            Print("Moving stop loss to entry as we hit breakeven");
    //            AdjustStopLossForLongPosition(_currentPosition.EntryPrice);
    //            _alreadyMovedToBreakEven = true;

    //            if (_closeHalfAtBreakEven)
    //            {
    //                _isClosingHalf = true;
    //                ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);                    
    //            }

    //            return true;
    //        }

    //        if (!ShouldTrail)
    //        {
    //            return true;
    //        }

    //        // Avoid adjusting trailing stop too often by adding a buffer
    //        var buffer = Symbol.PipSize * 3;

    //        //Print("Comparing current bid price of {0} to recent high {1}", Symbol.Bid, _recentHigh + buffer);
    //        if (Symbol.Ask > RecentHigh + buffer && _currentPosition.Pips > 0)
    //        {
    //            madeNewHigh = true;
    //            RecentHigh = Math.Max(Symbol.Ask, MarketSeries.High.Maximum(BarsSinceEntry + 1));
    //            Print("Recent high set to {0}", RecentHigh);
    //        }

    //        if (!madeNewHigh)
    //        {
    //            return true;
    //        }

    //        var stop = CalulateTrailingStopForLongPosition();
    //        AdjustStopLossForLongPosition(stop);

    //        return true;
    //    }

    //    private void AdjustStopLossForLongPosition(double? newStop)
    //    {
    //        if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value >= newStop.Value)
    //            return;

    //        ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
    //    }

    //    private double? CalulateTrailingStopForLongPosition()
    //    {
    //        double? stop = null;
    //        switch (_trailingStopLossRule)
    //        {
    //            case TrailingStopLossRule.StaticPipsValue:
    //                stop = Symbol.Ask - _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.CurrentBarNPips:
    //                stop = MarketSeries.Low.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.PreviousBarNPips:
    //                var low = Math.Min(MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
    //                stop = low - _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.ShortTermHighLow:
    //                stop = RecentHigh - _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.SmartProfitLocker:    
    //                stop = CalculateSmartTrailingStopForLong();                    
    //                break;
    //        }

    //        return stop;
    //    }

    //    /// <summary>
    //    /// Manages an existing short position.  Note this method is called on every tick.
    //    /// </summary>
    //    protected virtual bool ManageShortPosition()
    //    {
    //        if (BarsSinceEntry <= _barsToAllowTradeToDevelop) return false;

    //        if (_trailingStopLossRule == TrailingStopLossRule.None && !_moveToBreakEven) return true;

    //        // Are we making lower lows?
    //        var madeNewLow = false;

    //        if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Bid <= BreakEvenPrice)
    //        {
    //            Print("Moving stop loss to entry as we hit breakeven");
    //            AdjustStopLossForShortPosition(_currentPosition.EntryPrice);
    //            _alreadyMovedToBreakEven = true;

    //            if (_closeHalfAtBreakEven)
    //            {
    //                _isClosingHalf = true;
    //                ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
    //            }

    //            return true;
    //        }

    //        if (!ShouldTrail) return true;

    //        // Avoid adjusting trailing stop too often by adding a buffer
    //        var buffer = Symbol.PipSize * 3;

    //        //Print("Comparing current bid price of {0} to recent low {1}", Symbol.Bid, _recentLow - buffer);
    //        if (Symbol.Bid < RecentLow - buffer && _currentPosition.Pips > 0)
    //        {
    //            madeNewLow = true;
    //            RecentLow = Math.Min(Symbol.Bid, MarketSeries.Low.Minimum(BarsSinceEntry + 1));
    //            Print("Recent low set to {0}", RecentLow);
    //        }

    //        if (!madeNewLow) return true;

    //        var stop = CalulateTrailingStopForShortPosition();
    //        AdjustStopLossForShortPosition(stop);

    //        return true;
    //    }

    //    private void AdjustStopLossForShortPosition(double? newStop)
    //    {
    //        if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value <= newStop.Value)
    //            return;

    //        ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
    //    }

    //    private double? CalulateTrailingStopForShortPosition()
    //    {
    //        double? stop = null;
    //        switch (_trailingStopLossRule)
    //        {
    //            case TrailingStopLossRule.StaticPipsValue:
    //                stop = Symbol.Bid + _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.CurrentBarNPips:
    //                stop = MarketSeries.High.Last(1) + _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.PreviousBarNPips:
    //                var high = Math.Max(MarketSeries.High.Last(1), MarketSeries.High.Last(2));
    //                stop = high + _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.ShortTermHighLow:
    //                stop = RecentLow + _trailingStopLossInPips * Symbol.PipSize;
    //                break;

    //            case TrailingStopLossRule.SmartProfitLocker:
    //                stop = CalculateSmartTrailingStopForShort();
    //                break;
    //        }

    //        return stop;
    //    }

    //    private double? CalculateSmartTrailingStopForLong()
    //    {
    //        var minStop = 20;
    //        double stop;

    //        if (_currentPosition.Pips < minStop)
    //        {
    //            Print("Band 20");
    //            stop = minStop;
    //        }
    //        else if (_currentPosition.Pips < 40)
    //        {
    //            Print("Band 40");
    //            stop = 16;
    //        }
    //        else if (_currentPosition.Pips < 50)
    //        {
    //            Print("Band 50");
    //            stop = 12;
    //        }
    //        else
    //        {
    //            Print("Band MAX");
    //            stop = 8;
    //        }

    //        stop = RecentHigh - stop * Symbol.PipSize;
    //        return stop;
    //    }


    //    private double? CalculateSmartTrailingStopForShort()
    //    {
    //        var minStop = 20;
    //        double stop;

    //        if (_currentPosition.Pips < minStop)
    //        {
    //            Print("Band 20");
    //            stop = minStop;
    //        }
    //        else if (_currentPosition.Pips < 40)
    //        {
    //            Print("Band 40");
    //            stop = 16;
    //        }
    //        else if (_currentPosition.Pips < 50)
    //        {
    //            Print("Band 50");
    //            stop = 12;
    //        }
    //        else
    //        {
    //            Print("Band MAX");
    //            stop = 8;
    //        }

    //        stop = RecentLow + stop * Symbol.PipSize;
    //        return stop;
    //    }

    //    private bool ShouldWaitBeforeLookingForNewSetup()
    //    {
    //        if (_minutesToWaitAfterPositionClosed > 0 &&
    //            _lastClosedPositionTime != DateTime.MinValue &&
    //            Server.Time.Subtract(_lastClosedPositionTime).TotalMinutes <= _minutesToWaitAfterPositionClosed)
    //        {
    //            Print("Pausing before we look for new opportunities.");
    //            return true;
    //        }

    //        // Alternately, avoid trading on a Friday evening
    //        var openTime = MarketSeries.OpenTime.LastValue;
    //        if (openTime.DayOfWeek == DayOfWeek.Friday && openTime.Hour >= 16)
    //        {
    //            Print("Avoiding trading on a Friday afternoon");
    //            return true;
    //        }

    //        return false;
    //    }

    //    protected virtual void EnterLongPosition()
    //    {                        
    //        var stopLossPips = CalculateInitialStopLossInPipsForLongPosition();
    //        double lots;

    //        if (stopLossPips.HasValue)
    //        {
    //            lots = CalculatePositionQuantityInLots(stopLossPips.Value);
    //            Print("SL calculated for Buy order = {0}", stopLossPips);                
    //        }
    //        else
    //        {
    //            lots = 1;
    //        }

    //        var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
    //        ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
    //    }

    //    private double CalculatePositionQuantityInLots(double stopLossPips)
    //    {
    //        if (_lotSizingRule == LotSizingRule.Static)
    //        {
    //            return 1;
    //        }
           
    //        var risk = Account.Equity * _dynamicRiskPercentage / 100;
    //        var oneLotRisk = Symbol.PipValue * stopLossPips * Symbol.LotSize;
    //        var quantity = Math.Round(risk / oneLotRisk, 1);

    //        Print("Account Equity={0}, Risk={1}, Risk for one lot based on SL of {2} = {3}, Qty = {4}",
    //            Account.Equity, risk, stopLossPips, oneLotRisk, quantity);

    //        return quantity;
    //    }

    //    protected virtual double? CalculateInitialStopLossInPipsForLongPosition()
    //    {
    //        double? stopLossPips = null;

    //        switch (_initialStopLossRule)
    //        {
    //            case InitialStopLossRule.None:
    //                break;

    //            case InitialStopLossRule.StaticPipsValue:
    //                stopLossPips = _initialStopLossInPips;
    //                break;

    //            case InitialStopLossRule.CurrentBarNPips:
    //                stopLossPips = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
    //                break;

    //            case InitialStopLossRule.PreviousBarNPips:
    //                var low = MarketSeries.Low.Last(1);
    //                if (MarketSeries.Low.Last(2) < low)
    //                {
    //                    low = MarketSeries.Low.Last(2);
    //                }

    //                stopLossPips = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
    //                break;
    //        }

    //        if (stopLossPips.HasValue)
    //        {
    //            return Math.Round(stopLossPips.Value, 1);
    //        }

    //        return null;
    //    }

    //    private double? CalculateTakeProfit(double? stopLossPips)
    //    {
    //        switch (_takeProfitRule)
    //        {
    //            case TakeProfitRule.None:
    //                return null;

    //            case TakeProfitRule.StaticPipsValue:
    //                return _takeProfitInPips;

    //            case TakeProfitRule.DoubleRisk:
    //                return stopLossPips.HasValue 
    //                    ? stopLossPips.Value * 2 
    //                    : (double?)null;

    //            case TakeProfitRule.TripleRisk:
    //                return stopLossPips.HasValue
    //                    ? stopLossPips.Value * 3
    //                    : (double?)null;

    //            default:
    //                return null;
    //        }
    //    }

    //    protected virtual void EnterShortPosition()
    //    {            
    //        var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();
    //        double lots;

    //        if (stopLossPips.HasValue)
    //        {
    //            lots = CalculatePositionQuantityInLots(stopLossPips.Value);
    //            Print("SL calculated for Sell order = {0}", stopLossPips);                
    //        }
    //        else
    //        {
    //            lots = 1;
    //        }

    //        var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
    //        ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
    //    }

    //    protected virtual double? CalculateInitialStopLossInPipsForShortPosition()
    //    {
    //        double? stopLossPips = null;

    //        switch (_initialStopLossRule)
    //        {
    //            case InitialStopLossRule.None:
    //                break;

    //            case InitialStopLossRule.StaticPipsValue:
    //                stopLossPips = _initialStopLossInPips;
    //                break;

    //            case InitialStopLossRule.CurrentBarNPips:
    //                stopLossPips = _initialStopLossInPips + (MarketSeries.High.Last(1) - Symbol.Bid) / Symbol.PipSize;
    //                break;

    //            case InitialStopLossRule.PreviousBarNPips:
    //                var high = MarketSeries.High.Last(1);
    //                if (MarketSeries.High.Last(2) > high)
    //                {
    //                    high = MarketSeries.High.Last(2);
    //                }

    //                stopLossPips = _initialStopLossInPips + (high - Symbol.Bid) / Symbol.PipSize;
    //                break;
    //        }

    //        if (stopLossPips.HasValue)
    //        {
    //            return Math.Round(stopLossPips.Value, 1);
    //        }

    //        return null;

    //    }

    //    protected virtual void OnPositionOpened(PositionOpenedEventArgs args)
    //    {
    //        BarsSinceEntry = 0;
    //        RecentHigh = InitialRecentHigh;
    //        RecentLow = InitialRecentLow;
    //        _currentPosition = args.Position;
    //        var position = args.Position;
    //        var sl = position.StopLoss.HasValue
    //            ? string.Format(" (SL={0})", position.StopLoss.Value)
    //            : string.Empty;

    //        var tp = position.TakeProfit.HasValue
    //            ? string.Format(" (TP={0})", position.TakeProfit.Value)
    //            : string.Empty;

    //        Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);

    //        CalculateBreakEvenPrice();
    //        CalculateDoubleRiskPrice();
    //        CalculateTripleRiskPrice();
    //        CalculateTrailingInitiationPrice();

    //        _canOpenPosition = false;
    //        ShouldTrail = true;
    //    }

    //    private void CalculateBreakEvenPrice()
    //    {
    //        //Print("Current position's SL = {0}", _currentPosition.StopLoss.HasValue
    //        //    ? _currentPosition.StopLoss.Value.ToString()
    //        //    : "N/A");
    //        switch (_currentPosition.TradeType)
    //        {
    //            case TradeType.Buy:
    //                if (_currentPosition.StopLoss.HasValue)
    //                {
    //                    BreakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
    //                }
                    
    //                break;

    //            case TradeType.Sell:
    //                if (_currentPosition.StopLoss.HasValue)
    //                {
    //                    BreakEvenPrice = Symbol.Bid * 2 - _currentPosition.StopLoss.Value;
    //                }

    //                break;
    //        }
    //    }

    //    private void CalculateDoubleRiskPrice()
    //    {
    //        // Don't bother if we're never going to use it
    //        if (_takeProfitRule == TakeProfitRule.DoubleRisk)
    //        {
    //            DoubleRiskPrice = CalculateRiskPrice(2);
    //        }
    //    }

    //    private void CalculateTripleRiskPrice()
    //    {
    //        // Don't bother if we're never going to use it
    //        if (_takeProfitRule == TakeProfitRule.TripleRisk)
    //        {
    //            TripleRiskPrice = CalculateRiskPrice(3);
    //        }
    //    }

    //    private void CalculateTrailingInitiationPrice()
    //    {
    //        TrailingInitiationPrice = CalculateRiskPrice(0.75);
    //    }

    //    private double CalculateRiskPrice(double multiplier)
    //    {
    //        double diff;
    //        switch (_currentPosition.TradeType)
    //        {
    //            case TradeType.Buy:
    //                if (_currentPosition.StopLoss.HasValue)
    //                {
    //                    diff = _currentPosition.EntryPrice - _currentPosition.StopLoss.Value;
    //                    return _currentPosition.EntryPrice + (diff * multiplier);
    //                }

    //                break;

    //            case TradeType.Sell:
    //                if (_currentPosition.StopLoss.HasValue)
    //                {
    //                    diff = _currentPosition.StopLoss.Value - _currentPosition.EntryPrice;
    //                    return _currentPosition.EntryPrice - (diff * multiplier);
    //                }

    //                break;
    //        }

    //        return 0;
    //    }

    //    protected virtual void OnPositionClosed(PositionClosedEventArgs args)
    //    {
    //        _currentPosition = null;
    //        _alreadyMovedToBreakEven = false;

    //        ExitPrice = CalculateExitPrice(args.Position);
    //        PrintClosedPositionInfo(args.Position);

    //        _lastClosedPositionTime = Server.Time;

    //        _canOpenPosition = true;
    //    }

    //    private void OnPositionModified(PositionModifiedEventArgs args)
    //    {
    //        if (!_isClosingHalf)
    //            return;

    //        ExitPrice = CalculateExitPrice(args.Position);
    //        PrintClosedPositionInfo(args.Position);
    //        _isClosingHalf = false;
    //    }

    //    private void PrintClosedPositionInfo(Position position)
    //    {
    //        Print("Closed {0:N} {1} at {2} for {3} profit (pips={4})",
    //            position.VolumeInUnits, position.TradeType, ExitPrice, position.GrossProfit, position.Pips);            
    //    }

    //    private double CalculateExitPrice(Position position)
    //    {
    //        var diff = position.Pips * Symbol.PipSize;
    //        double exitPrice;
    //        if (position.TradeType == TradeType.Buy)
    //        {
    //            exitPrice = position.EntryPrice + diff;
    //        }
    //        else
    //        {
    //            exitPrice = position.EntryPrice - diff;
    //        }

    //        return 0;
    //    }
    //}

    public static class Common
    {
        public static int IndexOfLowestLow(DataSeries dataSeries, int periods)
        {
            var index = 1;
            var lowest = double.MaxValue;
            var lowestIndex = -1;

            while (index < periods)
            {
                var low = dataSeries.Last(index);
                if (low < lowest)
                {
                    lowest = low;
                    lowestIndex = index;
                }

                index++;
            }

            return lowestIndex;
        }

        public static double LowestLow(DataSeries dataSeries, int periods, int startIndex = 1)
        {
            var index = startIndex;
            var lowest = double.MaxValue;

            while (index < periods)
            {
                var low = dataSeries.Last(index);
                if (low < lowest)
                {
                    lowest = low;
                }

                index++;
            }

            return lowest;
        }
    }
}


