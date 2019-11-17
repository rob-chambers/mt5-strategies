using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace cAlgo.Library.Robots.VectorVestDowBot
{
    public enum SignalType
    {
        DewUp,
        DewDown
    }

    /*
     * RULES......................
     *  Enter on pull back to the 21MA on an H4
        Sell 1/2 on cross of 21MA on H4
        If RSI gets above 80, sell
        Close all on close below 89EMA

        Instead of closing when RSI hits 80, if a down bar, move SL to low of down bar
     */
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class VectorVestDowBot : BaseRobot
    {
        const string ConnectionString = @"Data Source = (localdb)\MSSQLLocalDB; Initial Catalog = cTrader; Integrated Security = True; Connect Timeout = 10; Encrypt = False;";

        [Serializable()]
        [XmlRoot("SignalCollection")]
        public class SignalDayCollection
        {
            [XmlArray("Signals")]
            [XmlArrayItem("Signal", typeof(SignalDay))]
            public SignalDay[] Signals { get; set; }
        }

        [Serializable()]
        public class SignalDay
        {
            [XmlElement("Type")]
            public SignalType SignalType { get; set; }

            [XmlIgnore]
            public DateTime Date { get; set; }

            [XmlElement("Date")]
            public string SomeDateString
            {
                get { return this.Date.ToString("yyyy-MM-dd HH:mm:ss"); }
                set { Date = DateTime.Parse(value); }
            }

            [XmlElement("RT")]
            public double RT { get; set; }

            [XmlElement("Bsr")]
            public double Bsr { get; set; }
        }

        #region Standard Parameters
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = 0)]
        public int InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = 0)]
        public int LotSizingRule { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 0)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Pause after position closed (Minutes)", DefaultValue = 0)]
        public int MinutesToWaitAfterPositionClosed { get; set; }

        [Parameter("Dynamic Risk Percentage?", DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        [Parameter("Number of bars to allow trade to develop", DefaultValue = 10)]
        public int BarsToAllowTradeToDevelop { get; set; }

        #endregion

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("Record", DefaultValue = false)]
        public bool RecordSession { get; set; }

        protected override string Name
        {
            get
            {
                return "VectorVest Dow";
            }
        }

        private MACrossOver _maCrossIndicator;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;        
        private int _runId;
        private int _currentPositionId;
        private RelativeStrengthIndex _rsi;
        private RelativeStrengthIndex _h4Rsi;
        private bool _closedHalf;
        private DateTime _upSignalDate;
        private DateTime _downSignalDate;
        private SignalDayCollection _signals;

        //private readonly List<SignalDay> _signals = new List<SignalDay>
        //{
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 5, 10),
        //        Bsr = 2.25,
        //        RT = 1.06
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 5, 25),
        //        Bsr = 1.50,
        //        RT = 1.04
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 6, 30),
        //        Bsr = 1.16,
        //        RT = 1.02
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 9, 22),
        //        Bsr = 2.22,
        //        RT = 1.07
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 10, 10),
        //        Bsr = 1.22,
        //        RT = 1.01
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2016, 11, 11),
        //        Bsr = 1.07,
        //        RT = 1.02
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 3, 30),
        //        Bsr = 1.48,
        //        RT = 1.02
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 4, 10),
        //        Bsr = 1.13,
        //        RT = 1.00
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 4, 17),
        //        Bsr = 1.04,
        //        RT = 0.99
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 4, 19),
        //        Bsr = 1.16,
        //        RT = 1.00
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 5, 22),
        //        Bsr = 1.10,
        //        RT = 0.99
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 7, 11),
        //        Bsr = 1.20,
        //        RT = 0.99
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 8, 31),
        //        Bsr = 1.15,
        //        RT = 1.01
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2017, 11, 16),
        //        Bsr = 1.02,
        //        RT = 1.00
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 3, 7),
        //        Bsr = 0.9,
        //        RT = 0.99
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 4, 17),
        //        Bsr = 1.08,
        //        RT = 0.99
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 4, 27),
        //        Bsr = 0.81,
        //        RT = 0.97
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 5, 9),
        //        Bsr = 1.21,
        //        RT = 1.01
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 7, 6),
        //        Bsr = 1.48,
        //        RT = 1.01
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 8, 2),
        //        Bsr = 0.79,
        //        RT = 0.96
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 8, 14),
        //        Bsr = 0.69,
        //        RT = 0.94
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 8, 17),
        //        Bsr = 0.69,
        //        RT = 0.94
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2018, 11, 28),
        //        Bsr = 0.29,
        //        RT = 0.89
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2019, 1, 11),
        //        Bsr = 0.64,
        //        RT = 0.98
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2019, 3, 28),
        //        Bsr = 1.66,
        //        RT = 1.05
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2019, 6, 13),
        //        Bsr = 0.46,
        //        RT = 0.93
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2019, 9, 5),
        //        Bsr = 0.55,
        //        RT = 0.95
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewUp,
        //        Date = new DateTime(2019, 10, 21),
        //        Bsr = 0.96,
        //        RT = 0.96
        //    },
        //    // ****************** DOWN SIGNALS
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2019, 10, 1),
        //        Bsr = 0.33,
        //        RT = 0.90
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2019, 8, 1),
        //        Bsr = 0.61,
        //        RT = 0.95
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2019, 5, 9),
        //        Bsr = 0.49,
        //        RT = 0.93
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2019, 3, 22),
        //        Bsr = 1.65,
        //        RT = 1.05
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 12, 4),
        //        Bsr = 0.19,
        //        RT = 0.85
        //    },
        //    new SignalDay
        //    {
        //        // BEST Signal
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 10, 1),
        //        Bsr = 0.63,
        //        RT = 0.95
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 8, 15),
        //        Bsr = 0.48,
        //        RT = 0.92
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 8, 13),
        //        Bsr = 0.56,
        //        RT = 0.93
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2019, 7, 30),
        //        Bsr = 0.66,
        //        RT = 0.95
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 6, 27),
        //        Bsr = 0.7,
        //        RT = 0.96
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 4, 30),
        //        Bsr = 0.69,
        //        RT = 0.96
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 4, 24),
        //        Bsr = 0.77,
        //        RT = 0.96
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 3, 22),
        //        Bsr = 0.4,
        //        RT = 0.93
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2018, 2, 5),
        //        Bsr = 0.34,
        //        RT = 0.91
        //    },
        //    new SignalDay
        //    {
        //        SignalType = SignalType.DewDown,
        //        Date = new DateTime(2017, 11, 13),
        //        Bsr = 1.15,
        //        RT = 1.00
        //    },
        //};

        protected override void OnStart()
        {
            ReadSignalData();

            _closedHalf = false;
            _maCrossIndicator = Indicators.GetIndicator<MACrossOver>(SourceSeries, SlowPeriodParameter, MediumPeriodParameter, FastPeriodParameter);
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            var h4series = MarketData.GetSeries(TimeFrame.Hour4);
            _rsi = Indicators.RelativeStrengthIndex(SourceSeries, 14);
            _h4Rsi = Indicators.RelativeStrengthIndex(h4series.Close, 14);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Initial SL in pips: {0}", InitialStopLossInPips);
            Print("Trailing SL in pips: {0}", TrailingStopLossInPips);
            Print("Lot sizing rule: {0}", LotSizingRule);
            Print("Take profit in pips: {0}", TakeProfitInPips);
            Print("Minutes to wait after position closed: {0}", MinutesToWaitAfterPositionClosed);
            Print("Recording: {0}", RecordSession);

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                InitialStopLossInPips,
                0,
                TrailingStopLossInPips,
                LotSizingRule,
                0,
                TakeProfitInPips,                
                MinutesToWaitAfterPositionClosed,
                false,
                false,
                DynamicRiskPercentage,
                BarsToAllowTradeToDevelop,
                0);

            Notifications.SendEmail("rechambers11@gmail.com", "rechambers11@gmail.com", "MA Cross Over robot initialized", "This is a test");

            if (RecordSession)
            {
                _runId = SaveRunToDatabase();
                if (_runId <= 0)
                    throw new InvalidOperationException("Run Id was <= 0!");
            }
        }

        private void ReadSignalData()
        {
            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktopFolder, "vv-signals.xml");
            var serializer = new XmlSerializer(typeof(SignalDayCollection));

            using (var reader = new StreamReader(filePath))
            {
                _signals = (SignalDayCollection)serializer.Deserialize(reader);               
            }
        }

        protected override void ValidateParameters(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            int initialStopLossRule, 
            int initialStopLossInPips, 
            int trailingStopLossRule, 
            int trailingStopLossInPips, 
            int lotSizingRule, 
            int takeProfitRule,
            int takeProfitInPips, 
            int minutesToWaitAfterPositionClosed, 
            bool moveToBreakEven, 
            bool closeHalfAtBreakEven,
            double dynamicRiskPercentage,
            int barsToAllowTradeToDevelop,
            int maCrossRule)
        {
            base.ValidateParameters(
                takeLongsParameter, 
                takeShortsParameter, 
                initialStopLossRule, 
                initialStopLossInPips, 
                trailingStopLossRule, 
                trailingStopLossInPips, 
                lotSizingRule,
                takeProfitRule,
                takeProfitInPips, 
                minutesToWaitAfterPositionClosed, 
                moveToBreakEven, 
                closeHalfAtBreakEven,
                dynamicRiskPercentage,
                barsToAllowTradeToDevelop,
                maCrossRule);

            if (FastPeriodParameter <= 0 || FastPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Fast MA Period' - must be between 1 and 999");

            if (MediumPeriodParameter <= 0 || MediumPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Medium MA Period' - must be between 1 and 999");

            if (SlowPeriodParameter <= 0 || SlowPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Slow MA Period' - must be between 1 and 999");

            if (!(FastPeriodParameter < MediumPeriodParameter && MediumPeriodParameter < SlowPeriodParameter))
                throw new ArgumentException("Invalid 'MA Periods' - fast must be less than medium and medium must be less than slow");
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            if (_initialStopLossRule == InitialStopLossRuleValues.Custom)
            {
                var stop = (Symbol.Ask - _mediumMA.Result.LastValue) / Symbol.PipSize;
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForLongPosition();
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            if (_initialStopLossRule == InitialStopLossRuleValues.Custom)
            {
                var stop = (_mediumMA.Result.LastValue - Symbol.Bid) / Symbol.PipSize;
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForShortPosition();
        }

        private int SaveRunToDatabase()
        {
            var sql = "INSERT INTO [dbo].[Run] (CreatedDate, Symbol, Timeframe, TakeLongs, TakeShorts," +
                            "InitialSLRule, InitialSLPips, TrailingSLRule, TrailingSLPips, LotSizingRule, TakeProfitPips," +
                            "PauseAfterPositionClosed, MoveToBreakEven, CloseHalfAtBreakEven," +
                            "MACrossThreshold, MACrossRule, H4MAPeriod" +
                            ") VALUES (@CreatedDate, @Symbol, @Timeframe, @TakeLongs, @TakeShorts," +
                            "@InitialSLRule, @InitialSLPips, @TrailingSLRule, @TrailingSLPips," +
                            "@LotSizingRule, @TakeProfitPips, @PauseAfterPositionClosed, @MoveToBreakEven, @CloseHalfAtBreakEven," +
                            "@MACrossThreshold, @MACrossRule, @H4MAPeriod" +
                            ");SELECT SCOPE_IDENTITY()";

            var identity = 0;

            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@Symbol", SymbolName);
                    command.Parameters.AddWithValue("@Timeframe", TimeFrame.ToString());
                    command.Parameters.AddWithValue("@TakeLongs", TakeLongsParameter);
                    command.Parameters.AddWithValue("@TakeShorts", TakeShortsParameter);
                    command.Parameters.AddWithValue("@InitialSLRule", InitialStopLossRule);
                    command.Parameters.AddWithValue("@InitialSLPips", InitialStopLossInPips);
                    command.Parameters.AddWithValue("@TrailingSLRule", 0);
                    command.Parameters.AddWithValue("@TrailingSLPips", TrailingStopLossInPips);
                    command.Parameters.AddWithValue("@LotSizingRule", LotSizingRule);
                    command.Parameters.AddWithValue("@TakeProfitPips", TakeProfitInPips);
                    command.Parameters.AddWithValue("@PauseAfterPositionClosed", MinutesToWaitAfterPositionClosed);
                    command.Parameters.AddWithValue("@MoveToBreakEven", false);
                    command.Parameters.AddWithValue("@CloseHalfAtBreakEven", false);
                    command.Parameters.AddWithValue("@MACrossThreshold", 0);
                    command.Parameters.AddWithValue("@MACrossRule", 0);
                    command.Parameters.AddWithValue("@H4MAPeriod", 0);

                    connection.Open();
                    identity = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }

            return identity;
        }

        protected override bool HasBullishSignal()
        {
            //Print("Current date: " + Server.Time.ToString("dd/MMM/yyyy HH:mm"));

            // Add a trading day onto the signal date because that is the first chance we will get to react to it
            if (_upSignalDate == DateTime.MinValue)
            {
                foreach (var signal in _signals.Signals.Where(x => x.SignalType == SignalType.DewUp))
                {
                    if (Server.Time.Date.CompareTo(signal.Date) == 0)
                    {
                        _upSignalDate = signal.Date;
                        break;
                    }
                }

                return false;
            }

            // Have we just passed a signal date, ticking over to the next trading day?
            if (Server.Time.Day != _upSignalDate.Day && Server.Time.Date.Subtract(_upSignalDate).TotalDays <= 4)
            {
                // Forget about waking up early in the morning!
                if (Server.Time.Hour <= 6)
                {
                    return false;
                }

                return true;
            }

            //var price = MarketSeries.Close.Last(1);
            //if (price > _fastMA.Result.LastValue)
            //{
            //    return true;
            //}

            return false;
        }

        protected override bool HasBearishSignal()
        {
            // Add a trading day onto the signal date because that is the first chance we will get to react to it
            if (_downSignalDate == DateTime.MinValue)
            {
                foreach (var signal in _signals.Signals.Where(x => x.SignalType == SignalType.DewDown))
                {
                    if (Server.Time.Date.CompareTo(signal.Date) == 0)
                    {
                        _downSignalDate = signal.Date;
                        break;
                    }
                }

                return false;
            }

            // Have we just passed a signal date, ticking over to the next trading day?
            if (Server.Time.Day != _downSignalDate.Day && Server.Time.Date.Subtract(_downSignalDate).TotalDays <= 4)
            {
                return true;
            }

            //var price = MarketSeries.Close.Last(1);
            //if (price < _fastMA.Result.LastValue)
            //{
            //    return true;
            //}

            return false;
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {
            _closedHalf = false;
            base.OnPositionOpened(args);
            ShouldTrail = false;

            if (RecordSession)
            {
                _currentPositionId = SaveOpenedPositionToDatabase(args.Position);
                if (_currentPositionId <= 0)
                    throw new InvalidOperationException("Position ID was <= 0!");
            }
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            base.OnPositionClosed(args);

            if (RecordSession)
                SaveClosedPositionToDatabase(args.Position);

            _upSignalDate = DateTime.MinValue;
        }

        private void SaveClosedPositionToDatabase(Position position)
        {
            var sql = "UPDATE [dbo].[Position] SET ExitTime=@ExitTime, GrossProfit=@GrossProfit, ExitPrice=@ExitPrice, Pips=@Pips" +
                " WHERE PositionId = @PositionId";

            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PositionId", _currentPositionId);
                    command.Parameters.AddWithValue("@ExitTime", Server.Time.ToUniversalTime());
                    command.Parameters.AddWithValue("@GrossProfit", position.GrossProfit);                    
                    command.Parameters.AddWithValue("@ExitPrice", ExitPrice);
                    command.Parameters.AddWithValue("@Pips", position.Pips);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        private int SaveOpenedPositionToDatabase(Position position)
        {
            var sql = "INSERT INTO [dbo].[Position] (RunID, EntryTime, TradeType, EntryPrice, Quantity, StopLoss, TakeProfit," +
                        "[Open], [High], [Low], [Close], [MA21], [MA55], [MA89], [RSI], [H4MA], [H4RSI]" +
                            ") VALUES (@RunId, @EntryTime, @TradeType, @EntryPrice, @Quantity, @StopLoss, @TakeProfit," +
                            "@Open, @High, @Low, @Close, @MA21, @MA55, @MA89, @RSI, @H4MA, @H4RSI);SELECT SCOPE_IDENTITY()";

            int identity;

            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RunId", _runId);
                    command.Parameters.AddWithValue("@EntryTime", position.EntryTime.ToUniversalTime());
                    command.Parameters.AddWithValue("@TradeType", position.TradeType.ToString());
                    command.Parameters.AddWithValue("@EntryPrice", position.EntryPrice);
                    command.Parameters.AddWithValue("@Quantity", position.Quantity);                    
                    command.Parameters.AddWithValue("@StopLoss", 
                        position.StopLoss.HasValue 
                            ? (object)position.StopLoss.Value 
                            : DBNull.Value);
                    command.Parameters.AddWithValue("@TakeProfit", 
                        position.TakeProfit.HasValue
                            ? (object)position.TakeProfit.Value
                            : DBNull.Value);

                    command.Parameters.AddWithValue("@Open", MarketSeries.Open.Last(1));
                    command.Parameters.AddWithValue("@High", MarketSeries.High.Last(1));
                    command.Parameters.AddWithValue("@Low", MarketSeries.Low.Last(1));
                    command.Parameters.AddWithValue("@Close", MarketSeries.Close.Last(1));
                    command.Parameters.AddWithValue("@MA21", _fastMA.Result.LastValue);
                    command.Parameters.AddWithValue("@MA55", _mediumMA.Result.LastValue);
                    command.Parameters.AddWithValue("@MA89", _slowMA.Result.LastValue);
                    command.Parameters.AddWithValue("@RSI", _rsi.Result.LastValue);
                    command.Parameters.AddWithValue("@H4MA", 0);
                    command.Parameters.AddWithValue("@H4RSI", _h4Rsi.Result.LastValue);

                    connection.Open();
                    identity = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }

            return identity;
        }

        protected override bool ManageLongPosition()
        {
            if (BarsSinceEntry <= BarsToAllowTradeToDevelop)
                return false;

            // Check if RSI is extended
            if (_rsi.Result.LastValue >= 80)
            {
                Print("Closing position now that the RSI is extended");
                _currentPosition.Close();
                return true;
            }

            // Check for a close below the fast MA
            var price = MarketSeries.Close.LastValue;
            if (!_closedHalf && price < _fastMA.Result.LastValue - 2 * Symbol.PipSize)
            {
                Print("Selling half now that we closed below the fast MA");
                ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                _closedHalf = true;
                return true;
            }

            // Check for a close below the slow MA
            if (price < _slowMA.Result.LastValue - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the slow MA");
                _currentPosition.Close();
                return true;
            }

            return true;
        }

        protected override bool ManageShortPosition()
        {
            if (BarsSinceEntry <= BarsToAllowTradeToDevelop)
                return false;

            // Check if RSI is extended
            if (_rsi.Result.LastValue <= 20)
            {
                Print("Closing position now that the RSI is extended");
                _currentPosition.Close();
                return true;
            }

            // Check for a close above the fast MA
            var price = MarketSeries.Close.LastValue;
            if (!_closedHalf && price > _fastMA.Result.LastValue + 2 * Symbol.PipSize)
            {
                Print("Closing half now that we closed above the fast MA");
                ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                _closedHalf = true;
                return true;
            }

            // Check for a close above the slow MA
            if (price > _slowMA.Result.LastValue + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the slow MA");
                _currentPosition.Close();
                return true;
            }

            return true;
        }
    }
}


