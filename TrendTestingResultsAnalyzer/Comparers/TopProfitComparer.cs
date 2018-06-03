using System.Collections;
using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Comparers
{
    public sealed class TopProfitComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var trade1 = x as TradeViewModel;
            var trade2 = y as TradeViewModel;

            return trade2.Profit.CompareTo(trade1.Profit);
        }
    }
}
