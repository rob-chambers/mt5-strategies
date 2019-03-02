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

        [Parameter("Initial SL Rule", DefaultValue = "ShortTermHighLow")]
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

        [Parameter("Stop Loss (pips)", DefaultValue = 30)]
        public int StopLossInPips { get; set; }

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
        private bool _canOpenPosition;

        protected override void OnStart()
        {
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Weighted);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            _canOpenPosition = true;

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Trailing SL rule: {0}", TrailingStopLossRule);

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                TrailingStopLossRule,
                LotSizingRule);
        }

        protected override void OnBar()
        {
            if (!_canOpenPosition)
            {
                return;
            }

            if (TakeLongsParameter && HasBullishSignal())
            {
                var Quantity = 1;

                var volumeInUnits = Symbol.QuantityToVolume(Quantity);
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, StopLossInPips, TakeProfitInPips);
            }
            else if (TakeShortsParameter && HasBearishSignal())
            {
                var Quantity = 1;

                var volumeInUnits = Symbol.QuantityToVolume(Quantity);
                ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, StopLossInPips, TakeProfitInPips);
            }
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

        private bool HasBullishSignal()
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

        private bool HasBearishSignal()
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
    }

    public abstract class BaseRobot : Robot
    {
        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private StopLossRule _initialStopLossRule;
        private StopLossRule _trailingStopLossRule;
        private LotSizingRule _lotSizingRule;

        //public int InitialSLPips { get; set; }

        //public int TrailingSLPips { get; set; }

        //public bool MoveToBreakEven { get; set; }

        protected abstract string Name { get; }

        protected void Init(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            string initialStopLossRule,
            string trailingStopLossRule,
            string lotSizingRule)
        {
            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), initialStopLossRule);
            _trailingStopLossRule = (StopLossRule)Enum.Parse(typeof(StopLossRule), trailingStopLossRule);
            _lotSizingRule = (LotSizingRule)Enum.Parse(typeof(LotSizingRule), lotSizingRule);
        }
    }
}
