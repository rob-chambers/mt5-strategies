using System;
using System.ComponentModel;
using System.Linq;
using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class CombineFilter : Filter
    {
        private FilterViewModel[] _allFilters;

        public CombineFilter(FilterViewModel[] allFilters)
        {
            _allFilters = allFilters;            
        }

        public override string Name => "Combination";

        public override string Description => "Select all filters to combine them";

        public override bool IsCombinable => false;

        public override bool IsIncluded(TradeViewModel trade)
        {
            return _allFilters.Where(x => x.Filter.IsCombinable && x.Filter.IsSelected).All(x => x.Filter.IsIncluded(trade));
        }
    }
}
