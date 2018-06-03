using FileHelpers;
using System;

namespace TrendTestingResultsAnalyzer.Model
{
    [DelimitedRecord(",")]
    [IgnoreFirst]
    public class Trade
    {
        // Deal	Entry Time	S/L	Entry	Exit Time	Exit	Profit	Open	High	Low	Close	MA50	MA240	Signal	MACD	Up Idx	Dn Idx	High 20	High 25	D Trend	RSI Current	RSI Prior	D MA1	D MA2
        // Deal Entry Time S/L Entry   Exit Time   Exit     Profit  Open    High    Low Close   MA50 MA240   Signal MACD    Up Idx  Dn Idx  High 20	High 25	Daily     Signal    D Up Idx D Dn Idx    D Trend RSI Current RSI Prior
        //2	2018.01.11 15:00:00	L	1.20041	2018.01.15 15:17:20	1.22824	3812.71	1.19435	1.2013	1.19364	1.20038	1.195867764	1.19909963	Up	0.003798688	1	19	1.2013	1.2018	Up	2	19	4	55.05995156	58.75649949


        public int DealNumber { get; set; }

        [FieldConverter(ConverterKind.Date, "yyyy.MM.dd HH:mm:ss")]
        public DateTime EntryDateTime { get; set; }

        [FieldConverter(typeof(DirectionConverter))]
        public TradeDirection Direction { get; set; }

        public double EntryPrice { get; set; }        

        [FieldConverter(ConverterKind.Date, "yyyy.MM.dd HH:mm:ss")]
        public DateTime ExitDateTime { get; set; }

        public double ExitPrice { get; set; }

        public double Profit { get; set; }

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }

        public double MA50 { get; set; }

        public double MA240 { get; set; }

        /*
         * Signal	MACD	Up Idx	Dn Idx	High 20	High 25	Daily Signal	D Up Idx	D Dn Idx	D Trend	RSI Current	RSI Prior
Up	0.003798688	1	19	1.2013	1.2018	Up	2	19	4	55.05995156	58.75649949
*/
        public string Signal { get; set; }

        public double MACD { get; set; }        

        public int UpIdx { get; set; }

        public int DownIdx { get; set; }

        public double High20 { get; set; }

        public double High25 { get; set; }

        public TrendType DailyTrend { get; set; }

        public double RsiCurrent { get; set; }

        public double RsiPrior { get; set; }

        public double DailyMA1 { get; set; }

        public double DailyMA2 { get; set; }

        //public int DailyUpIdx { get; set; }

        //public int DailyDnIdx { get; set; }

    }
}
