using cAlgo.API;

namespace Powder.TradingLibrary
{
    public static class Common
    {
        public static int IndexOfLowestLow(DataSeries dataSeries, int periods)
        {
            var index = 1;
            var lowest = double.MaxValue;
            var lowestIndex = -1;

            while (index < periods)
            {
                var low = dataSeries.Last(index);
                if (low < lowest)
                {
                    lowest = low;
                    lowestIndex = index;
                }

                index++;
            }

            return lowestIndex;
        }

        public static double LowestLow(DataSeries dataSeries, int periods, int startIndex = 1)
        {
            var index = startIndex;
            var lowest = double.MaxValue;

            while (index < periods)
            {
                var low = dataSeries.Last(index);
                if (low < lowest)
                {
                    lowest = low;
                }

                index++;
            }

            return lowest;
        }
    }
}
