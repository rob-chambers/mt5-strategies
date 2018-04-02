using FileHelpers;
using System;

namespace TestingResultsAnalyzer.Model
{
    [DelimitedRecord(",")]
    [IgnoreFirst]
    public class Trade
    {
        /*
Deal,Entry Time,S/L,Entry,Exit Time,Exit,Profit,MA50,MA100,MA240,MACD,H4 MA 0,H4 RSI 0,H4 MA 1,H4 RSI 1
2,2018.03.01 03:45:00,L,1.21899,2018.03.01 07:30:00,1.21927,20.36,1.22011547532604,1.221479656181522,1.223431999308438,-0.000418410476481057,1.222321699653567,26.69947957593519,1.223157124566959,27.14872282017235
4,2018.03.01 09:00:00,L,1.22055,2018.03.01 10:15:00,1.2198,-54.51,1.219687853976803,1.220747370841608,1.222338789764869,0.003230631447964427,1.221495647778283,32.75356901514002,1.221734559722854,27.36534539471562
         */

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

        public double MA50 { get; set; }

        public double MA100 { get; set; }

        public double MA240 { get; set; }

        public double MACD { get; set; }

        public double H4MA { get; set; }

        public double H4Rsi { get; set; }

        public double H4MA1 { get; set; }

        public double H4Rsi1 { get; set; }
    }
}
