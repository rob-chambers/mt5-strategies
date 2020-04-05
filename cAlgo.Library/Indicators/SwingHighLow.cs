using System.Collections;
using cAlgo.API;

namespace cAlgo.Library.Indicators
{
    /// <summary>
    /// Stolen from https://ctrader.com/algos/indicators/show/1107
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SwingHighLow : Indicator
    {
        #region Properties
        [Parameter("High source")]
        public DataSeries High { get; set; }

        [Parameter("Low source")]
        public DataSeries Low { get; set; }

        [Parameter("Strength", DefaultValue = 5, MinValue = 1)]
        public int strength { get; set; }

        [Output("Swing high", Color = Colors.Green, Thickness = 4, PlotType = PlotType.Points)]
        public IndicatorDataSeries SwingHighPlot { get; set; }

        [Output("Swing low", Color = Colors.Orange, Thickness = 4, PlotType = PlotType.Points)]
        public IndicatorDataSeries SwingLowPlot { get; set; }
        #endregion

        #region Variables
        private double currentSwingHigh = 0;
        private double currentSwingLow = 0;
        private double lastSwingHighValue = 0;
        private double lastSwingLowValue = 0;
        private int CurrentBar, Count;
        private int saveCurrentBar = -1;
        private ArrayList lastHighCache, lastLowCache;
        private IndicatorDataSeries swingHighSeries, swingHighSwings, swingLowSeries, swingLowSwings;
        #endregion

        protected override void Initialize()
        {
            lastHighCache = new ArrayList();
            lastLowCache = new ArrayList();

            swingHighSeries = CreateDataSeries();
            swingHighSwings = CreateDataSeries();
            swingLowSeries = CreateDataSeries();
            swingLowSwings = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            CurrentBar = index;
            Count = High.Count;

            if (saveCurrentBar != CurrentBar)
            {
                swingHighSwings[index] = 0;
                swingLowSwings[index] = 0;
                swingHighSeries[index] = 0;
                swingLowSeries[index] = 0;
                lastHighCache.Add(High.Last(0));

                if (lastHighCache.Count > (2 * strength) + 1)
                    lastHighCache.RemoveAt(0);
                lastLowCache.Add(Low.Last(0));
                if (lastLowCache.Count > (2 * strength) + 1)
                    lastLowCache.RemoveAt(0);

                if (lastHighCache.Count == (2 * strength) + 1)
                {
                    bool isSwingHigh = true;
                    double swingHighCandidateValue = (double)lastHighCache[strength];
                    for (int i = 0; i < strength; i++)
                        if ((double)lastHighCache[i] >= swingHighCandidateValue - double.Epsilon)
                            isSwingHigh = false;

                    for (int i = strength + 1; i < lastHighCache.Count; i++)
                        if ((double)lastHighCache[i] > swingHighCandidateValue - double.Epsilon)
                            isSwingHigh = false;

                    swingHighSwings[index - strength] = isSwingHigh ? swingHighCandidateValue : 0.0;
                    if (isSwingHigh)
                        lastSwingHighValue = swingHighCandidateValue;

                    if (isSwingHigh)
                    {
                        currentSwingHigh = swingHighCandidateValue;
                        for (int i = 0; i <= strength; i++)
                            SwingHighPlot[index - i] = currentSwingHigh;
                    }
                    else if (High.Last(0) > currentSwingHigh)
                    {
                        currentSwingHigh = 0.0;
                        SwingHighPlot[index] = double.NaN;
                    }
                    else
                        SwingHighPlot[index] = currentSwingHigh;

                    if (isSwingHigh)
                    {
                        for (int i = 0; i <= strength; i++)
                            swingHighSeries[index - i] = lastSwingHighValue;
                    }
                    else
                    {
                        swingHighSeries[index] = lastSwingHighValue;
                    }
                }

                if (lastLowCache.Count == (2 * strength) + 1)
                {
                    bool isSwingLow = true;
                    double swingLowCandidateValue = (double)lastLowCache[strength];
                    for (int i = 0; i < strength; i++)
                        if ((double)lastLowCache[i] <= swingLowCandidateValue + double.Epsilon)
                            isSwingLow = false;

                    for (int i = strength + 1; i < lastLowCache.Count; i++)
                        if ((double)lastLowCache[i] < swingLowCandidateValue + double.Epsilon)
                            isSwingLow = false;

                    swingLowSwings[index - strength] = isSwingLow ? swingLowCandidateValue : 0.0;
                    if (isSwingLow)
                        lastSwingLowValue = swingLowCandidateValue;

                    if (isSwingLow)
                    {
                        currentSwingLow = swingLowCandidateValue;
                        for (int i = 0; i <= strength; i++)
                            SwingLowPlot[index - i] = currentSwingLow;
                    }
                    else if (Low.Last(0) < currentSwingLow)
                    {
                        currentSwingLow = double.MaxValue;
                        SwingLowPlot[index] = double.NaN;
                    }
                    else
                        SwingLowPlot[index] = currentSwingLow;

                    if (isSwingLow)
                    {
                        for (int i = 0; i <= strength; i++)
                            swingLowSeries[index - i] = lastSwingLowValue;
                    }
                    else
                    {
                        swingLowSeries[index] = lastSwingLowValue;
                    }
                }

                saveCurrentBar = CurrentBar;
            }
            else
            {
                if (High.Last(0) > High.Last(strength) && swingHighSwings.Last(strength) > 0.0)
                {
                    swingHighSwings[index - strength] = 0.0;
                    for (int i = 0; i <= strength; i++)
                        SwingHighPlot[index - i] = double.NaN;
                    currentSwingHigh = 0.0;
                }
                else if (High.Last(0) > High.Last(strength) && currentSwingHigh != 0.0)
                {
                    SwingHighPlot[index] = double.NaN;
                    currentSwingHigh = 0.0;
                }
                else if (High.Last(0) <= currentSwingHigh)
                    SwingHighPlot[index] = currentSwingHigh;

                if (Low.Last(0) < Low.Last(strength) && swingLowSwings.Last(strength) > 0.0)
                {
                    swingLowSwings[index - strength] = 0.0;
                    for (int i = 0; i <= strength; i++)
                        SwingLowPlot[index - i] = double.NaN;
                    currentSwingLow = double.MaxValue;
                }
                else if (Low.Last(0) < Low.Last(strength) && currentSwingLow != double.MaxValue)
                {
                    SwingLowPlot[index] = double.NaN;
                    currentSwingLow = double.MaxValue;
                }
                else if (Low.Last(0) >= currentSwingLow)
                    SwingLowPlot[index] = currentSwingLow;
            }
        }
    }
}
