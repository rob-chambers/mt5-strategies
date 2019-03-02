using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Library.Robots
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
    public class BasicBot : BaseRobot
    {
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = true)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = "StaticPipsValue")]
        public string InitialStopLossRule { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = "None")]
        public string TrailingStopLossRule { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = "Static")]
        public string LotSizingRule { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 240)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 100)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 50)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 30)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 50)]
        public int TakeProfitInPips { get; set; }

        protected override string Name
        {
            get
            {
                return "Basic cBot";
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
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Weighted);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Trailing SL rule: {0}", TrailingStopLossRule);

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                TrailingStopLossRule,
                LotSizingRule,
                InitialStopLossInPips,
                TakeProfitInPips);
        }

        

        //protected override void OnTick()
        //{
        //    var longPosition = Positions.Find(Name, Symbol, TradeType.Buy);
        //    var shortPosition = Positions.Find(Name, Symbol, TradeType.Sell);

        //    if (longPosition == null || shortPosition == null)
        //    {
        //        _canOpenPosition = true;
        //        return;
        //    }

        //}        

        protected override bool HasBullishSignal()
        {
            var currentHigh = MarketSeries.High.Last(1);
            var currentClose = MarketSeries.Close.Last(1);
            var currentLow = MarketSeries.Low.Last(1);

            //Print("Checking for signal. - {0}, {1}, {2}", currentHigh, currentLow, currentClose);
            //if (currentHigh - currentClose > currentClose - currentLow) return false;

            //Print("Found bullish bar - HLC = {0}, {1}, {2}", currentHigh, currentLow, currentClose);

            // Special case first
            if (currentLow < _fastMA.Result.LastValue &&
                currentLow < _slowMA.Result.LastValue &&
                currentLow < _mediumMA.Result.LastValue &&
                currentHigh > _fastMA.Result.LastValue &&
                currentHigh > _mediumMA.Result.LastValue &&
                currentHigh > _slowMA.Result.LastValue)
            {
                Print("Found special case bar");
                return true;
            }

            return false;
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
    }

    public abstract class BaseRobot : Robot
    {
        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private StopLossRule _initialStopLossRule;
        private StopLossRule _trailingStopLossRule;
        private LotSizingRule _lotSizingRule;
        private int _initialStopLossInPips;
        private int _takeProfitInPips;
        private bool _canOpenPosition;

        //public bool MoveToBreakEven { get; set; }

        protected abstract string Name { get; }

        protected abstract bool HasBullishSignal();
        protected abstract bool HasBearishSignal();

        protected void Init(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            string initialStopLossRule,
            string trailingStopLossRule,
            string lotSizingRule,
            int initialStopLossInPips = 0,
            int takeProfitInPips = 0)
        {
            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), initialStopLossRule);
            _trailingStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), trailingStopLossRule);
            _lotSizingRule = (LotSizingRule)Enum.Parse(typeof(LotSizingRule), lotSizingRule);
            _initialStopLossInPips = initialStopLossInPips;
            _takeProfitInPips = takeProfitInPips;

            _canOpenPosition = true;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
        }

        protected override void OnBar()
        {
            if (!_canOpenPosition)
            {
                return;
            }

            double? stopLossLevel;
            if (_takeLongsParameter && HasBullishSignal())
            {
                var Quantity = 1;

                var volumeInUnits = Symbol.QuantityToVolume(Quantity);
                stopLossLevel = CalculateStopLossLevelForBuyOrder();
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossLevel, _takeProfitInPips);
            }
            else if (_takeShortsParameter && HasBearishSignal())
            {
                var Quantity = 1;

                var volumeInUnits = Symbol.QuantityToVolume(Quantity);
                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, _initialStopLossInPips, _takeProfitInPips);
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            Print("{0} {1:N} at {2}", position.TradeType, position.Volume, position.EntryPrice);
            _canOpenPosition = false;
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            Print("Closed {0:N} {1} at {2} for {3} profit", position.Volume, position.TradeType, position.EntryPrice, position.GrossProfit);
            _canOpenPosition = true;
        }

        private double? CalculateStopLossLevelForBuyOrder()
        {
            double? stopLossLevel = null;

            switch (_initialStopLossRule)
            {
                case StopLossRule.None:
                    break;

                case StopLossRule.StaticPipsValue:
                    stopLossLevel = _initialStopLossInPips;
                    break;

                case StopLossRule.CurrentBarNPips:
                    stopLossLevel = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
                    break;

                case StopLossRule.PreviousBarNPips:
                    var low = MarketSeries.Low.Last(1);
                    if (MarketSeries.Low.Last(2) < low)
                    {
                        low = MarketSeries.Low.Last(2);
                    }

                    stopLossLevel = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
                    break;
            }

            return stopLossLevel.HasValue
                ? (double?)Math.Round(stopLossLevel.Value, Symbol.Digits)
                : null;
        }
    }
}
