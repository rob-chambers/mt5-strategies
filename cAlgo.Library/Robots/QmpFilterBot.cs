// Version 2020-04-19 11:53
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System;

namespace cAlgo.Library.Robots.QmpFilterBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class QmpFilterBot : BaseRobot
    {
        [Parameter("Take long trades?", DefaultValue = false)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = LotSizingRuleValues.Static)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Dynamic Risk %age", DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Use Martingale?", DefaultValue = false)]
        public bool UseMartingale { get; set; }

        protected override string Name
        {
            get
            {
                return "QmpFilterBot";
            }
        }

        private QualitativeQuantitativeE _qqeAdv;
        private MovingAverage _mediumMA;
        private int _losingTradeCount;

        protected override void OnStart()
        {
            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);

            if (TakeLongsParameter && TakeShortsParameter)
            {
                throw new ArgumentException("This Robot is designed to either go long or short but not both at the same time");
            }
            else if (!TakeLongsParameter && !TakeShortsParameter)
            {
                throw new ArgumentException("You need to decide whether to go long or short");
            }

            Print("Lot sizing rule: {0}", LotSizingRule);

            var symbolLeverage = Symbol.DynamicLeverage[0].Leverage;
            Print("Symbol leverage: {0}", symbolLeverage);

            var realLeverage = Math.Min(symbolLeverage, Account.PreciseLeverage);
            Print("Account leverage: {0}", Account.PreciseLeverage);

            Init(TakeLongsParameter,
                TakeShortsParameter,
                InitialStopLossRuleValues.PreviousBarNPips,
                5,
                TrailingStopLossRuleValues.None,
                0,
                LotSizingRule,
                TakeProfitRuleValues.None,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                12);

            _qqeAdv = Indicators.GetIndicator<QualitativeQuantitativeE>(8);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
        }

        protected override bool HasBullishSignal()
        {
            var hasSignal = _qqeAdv.Result.Last(1) > _qqeAdv.ResultS.Last(1) &&
                _qqeAdv.Result.Last(2) <= _qqeAdv.ResultS.Last(2);

            if (hasSignal)
            {
                AlertService.SendAlert(new Alert("QMP Filter", Symbol.Name, Bars.TimeFrame.ToString()));
            }

            return hasSignal;
        }

        protected override bool HasBearishSignal()
        {
            var hasSignal = _qqeAdv.Result.Last(1) < _qqeAdv.ResultS.Last(1) &&
                _qqeAdv.Result.Last(2) >= _qqeAdv.ResultS.Last(2);

            if (hasSignal)
            {
                AlertService.SendAlert(new Alert("QMP Filter", Symbol.Name, Bars.TimeFrame.ToString()));
            }

            return hasSignal;
        }

        protected override bool ManageLongPosition()
        {
            // Important - call base functionality to check "bars to develop" functionality
            if (!base.ManageLongPosition()) return false;

            var value = _mediumMA.Result.LastValue;
            if (Bars.ClosePrices.Last(1) < value)
            {
                Print("Closing position now that we closed below the medium MA");
                _currentPosition.Close();
            }

            return true;
        }

        protected override bool ManageShortPosition()
        {
            // Important - call base functionality to check "bars to develop" functionality
            if (!base.ManageShortPosition()) return false;

            var value = _mediumMA.Result.LastValue;
            if (Bars.ClosePrices.Last(1) > value)
            {
                Print("Closing position now that we closed above the medium MA");
                _currentPosition.Close();
            }

            return true;
        }

        protected override double CalculatePositionQuantityInLots(double stopLossPips)
        {
            if (!UseMartingale)
            {
                return base.CalculatePositionQuantityInLots(stopLossPips);
            }

            Print("# losing trades: {0}", _losingTradeCount);

            const double BaseLots = 1;

            if (LotSizingRule == LotSizingRuleValues.Static)
            {
                return BaseLots * Math.Pow(2, _losingTradeCount);
            }

            var risk = Account.Equity * DynamicRiskPercentage / 100;
            var oneLotRisk = Symbol.PipValue * stopLossPips * Symbol.LotSize;
            var quantity = Math.Round(risk / oneLotRisk, 1);
            quantity *= Math.Pow(2, _losingTradeCount);

            Print("Account Equity={0}, Risk={1}, Risk for one lot based on SL of {2} = {3}, Qty = {4}",
                Account.Equity, risk, stopLossPips, oneLotRisk, quantity);

            return quantity;
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            base.OnPositionClosed(args);

            if (!UseMartingale)
                return;

            if (args.Position.GrossProfit < 0)
            {
                _losingTradeCount++;
            }
            else
            {
                _losingTradeCount = 0;
            }
        }
    }
}