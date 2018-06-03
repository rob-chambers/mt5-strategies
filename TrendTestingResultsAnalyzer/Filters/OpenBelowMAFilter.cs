using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class OpenBelowMAFilter : Filter
    {
        public override string Name => "Open Below long term MA";

        public override string Description => "Only take long trades when the open is below the 240 period MA";

        public override bool IsCombinable => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return trade.Direction == TradeDirection.Long
                ? trade.Open < trade.MA240
                : trade.Open > trade.MA240;
        }
    }
}
