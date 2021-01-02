// Version 2021-01-02 10:17
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;

namespace cAlgo.Library.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public class GoToDateIndicator : Indicator
    {
        [Parameter("Date")]
        public string InputDate { get; set; }

        private DateTime _inputDate;

        protected override void Initialize()
        {
            if (DateTime.TryParse(InputDate, out _inputDate))
            {
                GoToDate();
            }
            else
            {
                throw new Exception("Date is not valid.");
            }
        }

        private void GoToDate()
        {
            while (Bars.OpenTimes[0] > _inputDate)
                Bars.LoadMoreHistory();
            Chart.ScrollXTo(_inputDate);
        }

        public override void Calculate(int index)
        {              
        }
    }
}