// Version 2020-04-11 15:35
using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;

// ReSharper disable InconsistentNaming
// ReSharper disable UseStringInterpolation

namespace cAlgo.Library.Robots.QmpFilterBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class QmpFilterBot : BaseRobot
    {
        [Parameter("Take long trades?", DefaultValue = false)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = true)]
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

        protected override string Name 
        {
            get
            {
                return "QmpFilterBot";
            } 
        }

        private QualitativeQuantitativeE _qqeAdv;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;

        protected override void OnStart()
        {
            _qqeAdv = Indicators.GetIndicator<QualitativeQuantitativeE>(8);
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Lot sizing rule: {0}", LotSizingRule);

            Init(TakeLongsParameter,
                TakeShortsParameter,
                InitialStopLossRuleValues.PreviousBarNPips,
                2,
                TrailingStopLossRuleValues.None,
                0,
                LotSizingRule,
                TakeProfitRuleValues.None,
                0,
                0,
                false,
                false,
                DynamicRiskPercentage,
                5);
        }

        protected override bool HasBullishSignal()
        {
            return false;
        }

        protected override bool HasBearishSignal()
        {
            var signal =                
                _qqeAdv.Result.Last(1) < _qqeAdv.ResultS.Last(1) &&
                _qqeAdv.Result.Last(2) >= _qqeAdv.ResultS.Last(2);

            Print(signal);
            
            return signal;
        }
    }
}
