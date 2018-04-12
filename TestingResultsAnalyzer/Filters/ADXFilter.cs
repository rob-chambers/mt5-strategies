using TestingResultsAnalyzer.ViewModels;

namespace TestingResultsAnalyzer.Filters
{
    public class ADXFilter : Filter
    {
        public ADXFilter()
        {
            ArgumentValue = "23";
        }

        public override string Name => "ADX";

        public override string Description => "Only take trades when the ADX is above a certain value";

        public override bool IsCombinable => true;

        public override bool HasArgument => true;

        public override bool IsIncluded(TradeViewModel trade)
        {
            if (!double.TryParse(ArgumentValue, out double value))
            {
                return false;
            }

            return trade.ADX >= value;
        }
    }
}
