//using System;
//using cAlgo.API;
//using cAlgo.API.Indicators;
//using cAlgo.API.Internals;

//namespace cAlgo.Library.Robots
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
//    public class BasicBot : BaseRobot
//    {
//        [Parameter("Take long trades?", DefaultValue = true)]
//        public bool TakeLongsParameter { get; set; }

//        [Parameter("Take short trades?", DefaultValue = true)]
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

//        [Parameter("RSI Threshold", DefaultValue = 35, MinValue = 10, MaxValue = 60)]
//        public int RsiThreshold { get; set; }

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
//        private RelativeStrengthIndex _rsi;

//        protected override void OnStart()
//        {
//            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
//            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
//            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Weighted);
//            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
//            _rsi = Indicators.RelativeStrengthIndex(MarketSeries.Close, 14);

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

//        //protected override void OnTick()
//        //{
//        //    var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
//        //    var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

//        //    if (longPosition == null || shortPosition == null)
//        //    {
//        //        _canOpenPosition = true;
//        //        return;
//        //    }

//        //}        

//        protected override bool HasBullishSignal()
//        {
//            var currentHigh = MarketSeries.High.Last(1);
//            var currentClose = MarketSeries.Close.Last(1);
//            var currentLow = MarketSeries.Low.Last(1);

//            if (IsBullishPinBar(MarketSeries.Close.Count - 1))
//            {
//                Print("Found pin bar");
                
//                if (currentLow <= MarketSeries.Low.Minimum(10))
//                {
//                    Print("Found a low over the last 10 bars");
//                    if (_rsi.Result.Last(1) <= RsiThreshold || _rsi.Result.Last(2) <= RsiThreshold || _rsi.Result.Last(3) <= RsiThreshold)
//                    {
//                        Print("RSI recently went below {0}", RsiThreshold);
//                        return true;
//                    }
//                }

//            }

//            return false;
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

//    //public abstract class BaseRobot : Robot
//    //{
//    //    private bool _takeLongsParameter;
//    //    private bool _takeShortsParameter;
//    //    private StopLossRule _initialStopLossRule;
//    //    private StopLossRule _trailingStopLossRule;
//    //    private LotSizingRule _lotSizingRule;
//    //    private int _initialStopLossInPips;
//    //    private int _takeProfitInPips;
//    //    private bool _canOpenPosition;

//    //    //public bool MoveToBreakEven { get; set; }

//    //    protected abstract string Name { get; }

//    //    protected abstract bool HasBullishSignal();
//    //    protected abstract bool HasBearishSignal();

//    //    protected void Init(
//    //        bool takeLongsParameter, 
//    //        bool takeShortsParameter, 
//    //        string initialStopLossRule,
//    //        string trailingStopLossRule,
//    //        string lotSizingRule,
//    //        int initialStopLossInPips = 0,
//    //        int takeProfitInPips = 0)
//    //    {
//    //        _takeLongsParameter = takeLongsParameter;
//    //        _takeShortsParameter = takeShortsParameter;
//    //        _initialStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), initialStopLossRule);
//    //        _trailingStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), trailingStopLossRule);
//    //        _lotSizingRule = (LotSizingRule)Enum.Parse(typeof(LotSizingRule), lotSizingRule);
//    //        _initialStopLossInPips = initialStopLossInPips;
//    //        _takeProfitInPips = takeProfitInPips;

//    //        _canOpenPosition = true;

//    //        Positions.Opened += OnPositionOpened;
//    //        Positions.Closed += OnPositionClosed;

//    //        Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
//    //            Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
//    //    }

//    //    protected override void OnBar()
//    //    {
//    //        if (!_canOpenPosition)
//    //        {
//    //            return;
//    //        }

//    //        if (PendingOrders.Count > 0)
//    //        {
//    //            return;
//    //        }

//    //        double? stopLossLevel;
//    //        if (_takeLongsParameter && HasBullishSignal())
//    //        {
//    //            var Quantity = 1;

//    //            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
//    //            //stopLossLevel = CalculateStopLossLevelForBuyOrder();

//    //            var previousLow = MarketSeries.Low.Last(1);
//    //            Print("Last close = {0}", MarketSeries.Close.LastValue);
//    //            Print("Low of previous bar = {0}", previousLow);

//    //            stopLossLevel = (MarketSeries.Close.LastValue - previousLow) / Symbol.PipSize + _initialStopLossInPips;
//    //            Print("SL = {0}", stopLossLevel);

//    //            if (stopLossLevel.HasValue)
//    //            {
//    //                var targetPrice = MarketSeries.High.Maximum(2);

//    //                // Take profit at 1:1 risk
//    //                var takeProfitPips = stopLossLevel.Value;

//    //                // TODO: Fix expiration
//    //                var expiration = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(20), DateTimeKind.Utc);

//    //                PlaceStopOrder(TradeType.Buy, Symbol, volumeInUnits, targetPrice, Name, stopLossLevel, takeProfitPips, expiration, "Placing BUY Stop at " + targetPrice);
//    //            }

//    //            //ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossLevel, _takeProfitInPips);
//    //        }
//    //        else if (_takeShortsParameter && HasBearishSignal())
//    //        {
//    //            var Quantity = 1;

//    //            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
//    //            ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, _initialStopLossInPips, _takeProfitInPips);
//    //        }
//    //    }

//    //    private void OnPositionOpened(PositionOpenedEventArgs args)
//    //    {
//    //        var position = args.Position;
//    //        var sl = position.StopLoss.HasValue
//    //            ? string.Format(" (SL={0})", position.StopLoss.Value)
//    //            : string.Empty;

//    //        var tp = position.TakeProfit.HasValue
//    //            ? string.Format(" (TP={0})", position.TakeProfit.Value)
//    //            : string.Empty;

//    //        Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);
//    //        _canOpenPosition = false;
//    //    }

//    //    private void OnPositionClosed(PositionClosedEventArgs args)
//    //    {
//    //        var position = args.Position;
//    //        Print("Closed {0:N} {1} at {2} for {3} profit", position.VolumeInUnits, position.TradeType, position.EntryPrice, position.GrossProfit);
//    //        _canOpenPosition = true;
//    //    }

//    //    private double? CalculateStopLossLevelForBuyOrder()
//    //    {
//    //        double? stopLossLevel = null;

//    //        switch (_initialStopLossRule)
//    //        {
//    //            case StopLossRule.None:
//    //                break;

//    //            case StopLossRule.StaticPipsValue:
//    //                stopLossLevel = _initialStopLossInPips;
//    //                break;

//    //            case StopLossRule.CurrentBarNPips:
//    //                stopLossLevel = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
//    //                break;

//    //            case StopLossRule.PreviousBarNPips:
//    //                var low = MarketSeries.Low.Last(1);
//    //                if (MarketSeries.Low.Last(2) < low)
//    //                {
//    //                    low = MarketSeries.Low.Last(2);
//    //                }

//    //                stopLossLevel = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
//    //                break;
//    //        }

//    //        return stopLossLevel.HasValue
//    //            ? (double?)Math.Round(stopLossLevel.Value, Symbol.Digits)
//    //            : null;
//    //    }
//    //}
//}
