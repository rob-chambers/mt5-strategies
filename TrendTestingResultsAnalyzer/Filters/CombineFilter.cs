using System;
using System.ComponentModel;
using System.Linq;
using TrendTestingResultsAnalyzer.ViewModels;

namespace TrendTestingResultsAnalyzer.Filters
{
    public class CombineFilter : Filter
    {
        private FilterViewModel[] _allFilters;

        public CombineFilter(FilterViewModel[] allFilters)
        {
            _allFilters = allFilters;            
        }

        public override string Name => "Combination";

        public override string Description => "Check all filters to combine them";

        public override bool IsCombinable => false;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return _allFilters.Where(x => x.Filter.IsCombinable && x.Filter.IsChecked).All(x => x.Filter.IsIncluded(trade));
        }
    }
}
