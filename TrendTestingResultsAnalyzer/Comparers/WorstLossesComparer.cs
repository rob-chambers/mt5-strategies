using System.Collections;
using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Comparers
{
    public sealed class WorstLossesComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var trade1 = x as TradeViewModel;
            var trade2 = y as TradeViewModel;

            return trade1.Profit.CompareTo(trade2.Profit);
        }
    }
}
