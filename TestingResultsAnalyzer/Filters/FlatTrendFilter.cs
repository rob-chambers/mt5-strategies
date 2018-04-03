using System;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class FlatTrendFilter : Filter
    {
        public override string Name => "Flat Trend";

        public override string Description => "Avoid taking trades when the trend is flat.";

        public override bool IsIncluded(TradeViewModel trade)
        {
            // TODO: Requirements??
            return true;
        }
    }
}
