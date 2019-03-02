using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Library.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BasicBot : Robot
    {
        private const string Name = "Basic cBot";

        [Parameter()]
        public DataSeries SourceSeries { get; set; }
       
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = true)]
        public bool TakeShortsParameter { get; set; }

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

            //double currentHigh = MarketSeries.High[1];
            //double currentClose = MarketSeries.Close[1];
            //double currentLow = MarketSeries.Low[1];

            //Print("HLC: {0},{1},{2}", currentHigh, currentLow, currentClose);

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            _canOpenPosition = true;
        }

        protected override void OnBar()
        {
            if (!_canOpenPosition)
            {
                return;
            }

            if (HasBullishSignal())
            {
                var Quantity = 1;

                var volumeInUnits = Symbol.QuantityToVolume(Quantity);
                ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, StopLossInPips, TakeProfitInPips);
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
}
