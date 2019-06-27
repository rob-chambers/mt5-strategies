using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;
using System.Linq;

namespace cAlgo.Library.Robots.NewBreakout
{
    public enum InitialStopLossRule
    {
        None,
        CurrentBarNPips,
        PreviousBarNPips,
        StaticPipsValue,
        Custom
    };

    public enum TrailingStopLossRule
    {
        None,        
        CurrentBarNPips,
        PreviousBarNPips,
        ShortTermHighLow,
        StaticPipsValue,
        SmartProfitLocker
    };

    public enum TakeProfitRule
    {
        None,
        StaticPipsValue,
        DoubleRisk,
        TripleRisk
    }

    public enum LotSizingRule
    {
        Static,
        Dynamic
    };

    public enum MaCrossRule
    {
        None,
        CloseOnFastMaCross,
        CloseOnMediumMaCross
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class NewBreakoutBot : BaseRobot
    {
        const string ConnectionString = @"Data Source = (localdb)\MSSQLLocalDB; Initial Catalog = cTrader; Integrated Security = True; Connect Timeout = 10; Encrypt = False;";

        #region Standard Parameters
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = true)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = 2)]
        public int InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = 0)]
        public int TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = 0)]
        public int LotSizingRule { get; set; }

        [Parameter("Take Profit Rule", DefaultValue = 0)]
        public int TakeProfitRule { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 0)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Pause after position closed (Minutes)", DefaultValue = 0)]
        public int MinutesToWaitAfterPositionClosed { get; set; }

        [Parameter("Move to breakeven?", DefaultValue = false)]
        public bool MoveToBreakEven { get; set; }

        [Parameter("Close half at breakeven?", DefaultValue = false)]
        public bool CloseHalfAtBreakEven { get; set; }

        [Parameter("Dynamic Risk Percentage?", DefaultValue = 2)]
        public double DynamicRiskPercentage { get; set; }

        #endregion

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow MA Period", DefaultValue = 89)]
        public int SlowPeriodParameter { get; set; }

        [Parameter("Medium MA Period", DefaultValue = 55)]
        public int MediumPeriodParameter { get; set; }

        [Parameter("Fast MA Period", DefaultValue = 21)]
        public int FastPeriodParameter { get; set; }

        [Parameter("H4 MA Period", DefaultValue = 21)]
        public int H4MaPeriodParameter { get; set; }

        [Parameter("MAs Cross Threshold (# bars)", DefaultValue = 30)]
        public int MovingAveragesCrossThreshold { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = 1)]
        public int MaCrossRule { get; set; }

        [Parameter("Record", DefaultValue = false)]
        public bool RecordSession { get; set; }

        [Parameter("Enter at Market", DefaultValue = true)]
        public bool EnterAtMarket { get; set; }

        [Parameter("Apply closing vs prior close filter", DefaultValue = true)]
        public bool CloseVsPriorCloseFilter { get; set; }

        [Parameter("Apply close vs open filter", DefaultValue = true)]
        public bool CloseVsOpenFilter { get; set; }

        [Parameter("Apply high/low vs prior high/low filter", DefaultValue = true)]
        public bool HighLowVsPriorHighLowFilter { get; set; }

        [Parameter("Apply MA Distance filter", DefaultValue = true)]
        public bool MADistanceFilter { get; set; }

        [Parameter("Apply MA Max Distance filter", DefaultValue = true)]
        public bool MAMaxDistanceFilter { get; set; }

        [Parameter("Apply Flat MAs filter", DefaultValue = true)]
        public bool MAsFlatFilter { get; set; }

        [Parameter("New high/low filter", DefaultValue = true)]
        public bool NewHighLowFilter { get; set; }

        protected override string Name
        {
            get
            {
                return "NewBreakoutBot";
            }
        }

        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;
        private MaCrossRule _maCrossRule;
        private int _runId;
        private int _currentPositionId;
        private ExponentialMovingAverage _h4Ma;
        private RelativeStrengthIndex _rsi;
        private RelativeStrengthIndex _h4Rsi;

        protected override void OnStart()
        {
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            var h4series = MarketData.GetSeries(TimeFrame.Hour4);
            _h4Ma = Indicators.ExponentialMovingAverage(h4series.Close, H4MaPeriodParameter);
            _rsi = Indicators.RelativeStrengthIndex(SourceSeries, 14);
            _h4Rsi = Indicators.RelativeStrengthIndex(h4series.Close, 14);

            Print("Take Longs: {0}", TakeLongsParameter);
            Print("Take Shorts: {0}", TakeShortsParameter);
            Print("Initial SL rule: {0}", InitialStopLossRule);
            Print("Initial SL in pips: {0}", InitialStopLossInPips);
            Print("Trailing SL rule: {0}", TrailingStopLossRule);
            Print("Trailing SL in pips: {0}", TrailingStopLossInPips);
            Print("Lot sizing rule: {0}", LotSizingRule);
            Print("Take profit rule: {0}", TakeProfitRule);
            Print("Take profit in pips: {0}", TakeProfitInPips);
            Print("Minutes to wait after position closed: {0}", MinutesToWaitAfterPositionClosed);
            Print("Move to breakeven: {0}", MoveToBreakEven);
            Print("Close half at breakeven: {0}", CloseHalfAtBreakEven);
            Print("MA Cross Rule: {0}", MaCrossRule);
            Print("H4MA: {0}", H4MaPeriodParameter);
            Print("Recording: {0}", RecordSession);
            Print("Enter at Market: {0}", EnterAtMarket);
            Print("CloseVsPriorCloseFilter: {0}", CloseVsPriorCloseFilter);
            Print("CloseVsOpenFilter: {0}", CloseVsOpenFilter);
            Print("HighVsPriorHighFilter: {0}", HighLowVsPriorHighLowFilter);
            Print("MADistanceFilter: {0}", MADistanceFilter);
            Print("MAsFlatFilter: {0}", MAsFlatFilter);
            Print("NewHighLowFilter: {0}", NewHighLowFilter);

            _maCrossRule = (MaCrossRule)MaCrossRule;

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
                TakeProfitRule,
                TakeProfitInPips,                
                MinutesToWaitAfterPositionClosed,
                MoveToBreakEven,
                CloseHalfAtBreakEven,
                DynamicRiskPercentage);

            if (RecordSession)
            {
                _runId = SaveRunToDatabase();
                if (_runId <= 0)
                {
                    throw new InvalidOperationException("Run Id was <= 0!");
                }
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
            double dynamicRiskPercentage)
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
                dynamicRiskPercentage);

            if (FastPeriodParameter <= 0 || FastPeriodParameter > 999)
            {
                throw new ArgumentException("Invalid 'Fast MA Period' - must be between 1 and 999");
            }

            if (MediumPeriodParameter <= 0 || MediumPeriodParameter > 999)
            {
                throw new ArgumentException("Invalid 'Medium MA Period' - must be between 1 and 999");
            }

            if (SlowPeriodParameter <= 0 || SlowPeriodParameter > 999)
            {
                throw new ArgumentException("Invalid 'Slow MA Period' - must be between 1 and 999");
            }

            if (!(FastPeriodParameter < MediumPeriodParameter && MediumPeriodParameter < SlowPeriodParameter))
            {
                throw new ArgumentException("Invalid 'MA Periods' - fast must be less than medium and medium must be less than slow");
            }

            if (MovingAveragesCrossThreshold <= 0 || MovingAveragesCrossThreshold > 999)
            {
                throw new ArgumentException("MAs Cross Threshold - must be between 1 and 999");
            }

            if (!Enum.IsDefined(typeof(MaCrossRule), MaCrossRule))
            {
                throw new ArgumentException("Invalid MA Cross rule");
            }

            var slRule = (InitialStopLossRule)initialStopLossRule;
            var rule = (TrailingStopLossRule)trailingStopLossRule;

            if (_maCrossRule == NewBreakout.MaCrossRule.None && slRule == NewBreakout.InitialStopLossRule.None && rule == NewBreakout.TrailingStopLossRule.None)
            {
                throw new ArgumentException("The combination of parameters means that a position may incur a massive loss");
            }

            if (H4MaPeriodParameter < 10 || H4MaPeriodParameter > 99)
            {
                throw new ArgumentException("H4 MA Period must be between 10 and 99");
            }
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            if (_initialStopLossRule == NewBreakout.InitialStopLossRule.Custom)
            {
                var stop = (Symbol.Ask - _mediumMA.Result.LastValue) / Symbol.PipSize;
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForLongPosition();
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            if (_initialStopLossRule == NewBreakout.InitialStopLossRule.Custom)
            {
                var stop = (_mediumMA.Result.LastValue - Symbol.Bid) / Symbol.PipSize;
                return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForShortPosition();
        }

        private double? CalculateFibTakeProfit()
        {
            return null;


            // Find highest high from here back
            //var highest = MarketSeries.High.Maximum(20);
           
            //const double StandardFib = 1.382;

            //var lowestLow = Common.LowestLow(MarketSeries.Low, 10, 6);
            //var level = Math.Round((highest - lowestLow) * StandardFib / Symbol.PipSize, 1);

            //Print("Fib TP calculation = {0} based on low of {1} and high of {2}",
            //    level, lowestLow, highest);

            //return level;
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
                    command.Parameters.AddWithValue("@Symbol", Symbol.Code);
                    command.Parameters.AddWithValue("@Timeframe", TimeFrame.ToString());
                    command.Parameters.AddWithValue("@TakeLongs", TakeLongsParameter);
                    command.Parameters.AddWithValue("@TakeShorts", TakeShortsParameter);
                    command.Parameters.AddWithValue("@InitialSLRule", InitialStopLossRule);
                    command.Parameters.AddWithValue("@InitialSLPips", InitialStopLossInPips);
                    command.Parameters.AddWithValue("@TrailingSLRule", TrailingStopLossRule);
                    command.Parameters.AddWithValue("@TrailingSLPips", TrailingStopLossInPips);
                    command.Parameters.AddWithValue("@LotSizingRule", LotSizingRule);
                    command.Parameters.AddWithValue("@TakeProfitPips", TakeProfitInPips);
                    command.Parameters.AddWithValue("@PauseAfterPositionClosed", MinutesToWaitAfterPositionClosed);
                    command.Parameters.AddWithValue("@MoveToBreakEven", MoveToBreakEven);
                    command.Parameters.AddWithValue("@CloseHalfAtBreakEven", CloseHalfAtBreakEven);
                    command.Parameters.AddWithValue("@MACrossThreshold", MovingAveragesCrossThreshold);
                    command.Parameters.AddWithValue("@MACrossRule", MaCrossRule);
                    command.Parameters.AddWithValue("@H4MAPeriod", H4MaPeriodParameter);

                    connection.Open();
                    identity = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }

            return identity;
        }

        protected override bool HasBullishSignal()
        {
            for (var index = 1; index < 70; index++)
            {
                if (!AreMovingAveragesFlatAtIndex(index))
                {
                    return false;
                }
            }

            const int HighestHighPeriod = 70;

            var high = MarketSeries.High.Maximum(HighestHighPeriod);
            var priorHigh = MarketSeries.High.Last(1);
            if (priorHigh != high)
            {
                //Print("Setup rejected as the prior high {0} has not gone higher than {1}", priorHigh, high);
                return false;
            }

            return true;
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {
            base.OnPositionOpened(args);
            ShouldTrail = false;

            Print("Trailing will be initiated if price reaches {0}", TrailingInitiationPrice);

            if (RecordSession)
            {
                _currentPositionId = SaveOpenedPositionToDatabase(args.Position);
                if (_currentPositionId <= 0)
                {
                    throw new InvalidOperationException("Position ID was <= 0!");
                }
            }
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            base.OnPositionClosed(args);

            if (RecordSession)
            {
                SaveClosedPositionToDatabase(args.Position);
            }
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
                    command.Parameters.AddWithValue("@H4MA", _h4Ma.Result.LastValue);
                    command.Parameters.AddWithValue("@H4RSI", _h4Rsi.Result.LastValue);

                    connection.Open();
                    identity = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }

            return identity;
        }

        private bool AreMovingAveragesFlatAtIndex(int index)
        {
            return Math.Abs(_fastMA.Result.Last(index) - _mediumMA.Result.Last(index)) / Symbol.PipSize <= 20 &&
                   Math.Abs(_mediumMA.Result.Last(index) - _slowMA.Result.Last(index)) / Symbol.PipSize <= 20 &&
                   Math.Abs(_fastMA.Result.Last(index) - _slowMA.Result.Last(index)) / Symbol.PipSize <= 20;
        }

        private bool AreMovingAveragesStackedBearishlyAtIndex(int index)
        {
            return _fastMA.Result.Last(index) < _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) < _slowMA.Result.Last(index);
        }

        protected override bool HasBearishSignal()
        {
            /* RULES
            1) Fast MA < Medium MA < Slow MA (MAs are 'stacked')
            2) Crossing of MAs must have occurred in the last n bars
            3) Close < Fast MA
            4) Close < Prior close
            5) Close < Open
            6) Low < Prior low
             */
            //if (!AreMovingAveragesStackedBearishly())
            //{
            //    return false;
            //}

            //var lastCross = GetLastBearishBowtie();
            //if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
            //{
            //    // Either there was no cross or it was too long ago and we have missed the move
            //    return false;
            //}

            //Print("Bearish cross identified at index {0}", lastCross);

            //if (MarketSeries.Close.LastValue >= _fastMA.Result.LastValue)
            //{
            //    //Print("Setup rejected as we closed higher than the fast MA");
            //    return false;
            //}

            //if (CloseVsPriorCloseFilter && MarketSeries.Close.Last(1) >= MarketSeries.Close.Last(2))
            //{
            //    //Print("Setup rejected as we closed higher than the prior close ({0} vs {1})",
            //    //    MarketSeries.Close.Last(1), MarketSeries.Close.Last(2));
            //    return false;
            //}

            //if (CloseVsOpenFilter && MarketSeries.Close.Last(1) >= MarketSeries.Open.Last(1))
            //{
            //    //Print("Setup rejected as we closed higher than the open ({0} vs {1})",
            //    //    MarketSeries.Close.Last(1), MarketSeries.Open.Last(1));
            //    return false;
            //}

            //if (HighLowVsPriorHighLowFilter && MarketSeries.Low.Last(1) >= MarketSeries.Low.Last(2))
            //{
            //    //Print("Setup rejected as the low wasn't lower than the prior low ({0} vs {1})",
            //    //    MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
            //    return false;
            //}

            //// Another filter - what's the distance between the MAs?  Avoid noise and ensure there's been a breakout
            //if (MADistanceFilter && (_mediumMA.Result.LastValue - _fastMA.Result.LastValue) / Symbol.PipSize < 7)
            //{
            //    Print("Setup rejected as there wasn't enough distance between the fast and medium MAs");
            //    return false;
            //}

            //// What's the distance between the MAs?  Ensure we haven't already missed the move
            //if (MAMaxDistanceFilter && (_mediumMA.Result.LastValue - _fastMA.Result.LastValue) / Symbol.PipSize >= 30)
            //{
            //    Print("Setup rejected as the distance between the fast and medium MAs was more than 30 pips");
            //    return false;
            //}

            //// How high was the recent highest high?  Attempt to only enter when the MAs have been flat
            //if (MAsFlatFilter && !MAsShouldAreFlatForBearishSetup())
            //{
            //    Print("Setup rejected as the MAs don't seem to be flat");
            //    return false;
            //}

            //if (NewHighLowFilter)
            //{
            //    // Another filter - have we hit a new low?
            //    const int LowestLowPeriod = 70;

            //    var low = MarketSeries.Low.Minimum(LowestLowPeriod);
            //    var priorLow = MarketSeries.Low.Last(1);
            //    if (priorLow != low)
            //    {
            //        Print("Setup rejected as the prior low {0} has not gone lower than {1}", priorLow, low);
            //        return false;
            //    }

            //    _lowestLow = low;
            //}

            return false;
        }

        protected override void ManageLongPosition()
        {
            if (!ShouldTrail && Symbol.Ask > TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop higher
            base.ManageLongPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case NewBreakout.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case NewBreakout.MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return;
            }

            if (MarketSeries.Close.Last(1) < value - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the {0} MA", maType);
                _currentPosition.Close();
            }
        }

        protected override void ManageShortPosition()
        {
            if (!ShouldTrail && Symbol.Bid < TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop lower
            base.ManageShortPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case NewBreakout.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case NewBreakout.MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return;
            }

            if (MarketSeries.Close.Last(1) > value + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the {0} MA", maType);
                _currentPosition.Close();
            }
        }
    }

    public abstract class BaseRobot : Robot
    {
        protected const int InitialRecentLow = int.MaxValue;
        protected const int InitialRecentHigh = 0;

        protected abstract string Name { get; }
        protected Position _currentPosition;
        protected double ExitPrice { get; private set; }
        protected int BarsSinceEntry { get; private set; }
        protected double RecentLow { get; set; }
        protected double RecentHigh { get; set; }
        protected bool ShouldTrail { get; set; }
        protected double BreakEvenPrice { get; private set; }
        protected double DoubleRiskPrice { get; private set; }
        protected double TripleRiskPrice { get; private set; }
        protected double? TrailingInitiationPrice { get; private set; }

        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        protected InitialStopLossRule _initialStopLossRule;
        private TrailingStopLossRule _trailingStopLossRule;
        private LotSizingRule _lotSizingRule;
        private int _initialStopLossInPips;
        private TakeProfitRule _takeProfitRule;
        private int _takeProfitInPips;
        private int _trailingStopLossInPips;
        private int _minutesToWaitAfterPositionClosed;
        private bool _moveToBreakEven;
        private bool _closeHalfAtBreakEven;
        private double _dynamicRiskPercentage;
        private bool _canOpenPosition;
        private DateTime _lastClosedPositionTime;
        private bool _alreadyMovedToBreakEven;        
        private bool _isClosingHalf;

        protected abstract bool HasBullishSignal();
        protected abstract bool HasBearishSignal();

        protected void Init(
            bool takeLongsParameter, 
            bool takeShortsParameter, 
            int initialStopLossRule,
            int initialStopLossInPips,
            int trailingStopLossRule,
            int trailingStopLossInPips,
            int lotSizingRule,         
            int takeProfitRule,
            int takeProfitInPips = 0,            
            int minutesToWaitAfterPositionClosed = 0,
            bool moveToBreakEven = false,
            bool closeHalfAtBreakEven = false,
            double dynamicRiskPercentage = 2)
        {
            ValidateParameters(takeLongsParameter, takeShortsParameter, initialStopLossRule, initialStopLossInPips,
                    trailingStopLossRule, trailingStopLossInPips, lotSizingRule, takeProfitRule, takeProfitInPips,
                    minutesToWaitAfterPositionClosed, moveToBreakEven, closeHalfAtBreakEven, dynamicRiskPercentage);

            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = (InitialStopLossRule)initialStopLossRule;
            _initialStopLossInPips = initialStopLossInPips;
            _trailingStopLossRule = (TrailingStopLossRule)trailingStopLossRule;
            _trailingStopLossInPips = trailingStopLossInPips;
            _lotSizingRule = (LotSizingRule)lotSizingRule;
            _takeProfitRule = (TakeProfitRule)takeProfitRule;
            _takeProfitInPips = takeProfitInPips;
            _minutesToWaitAfterPositionClosed = minutesToWaitAfterPositionClosed;
            _moveToBreakEven = moveToBreakEven;
            _closeHalfAtBreakEven = closeHalfAtBreakEven;
            _dynamicRiskPercentage = dynamicRiskPercentage;

            _canOpenPosition = true;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            Positions.Modified += OnPositionModified;

            Print("Symbol.TickSize: {0}, Symbol.Digits: {1}, Symbol.PipSize: {2}", 
                Symbol.TickSize, Symbol.Digits, Symbol.PipSize);
        }

        protected virtual void ValidateParameters(
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
            double dynamicRiskPercentage)
        {
            if (!takeLongsParameter && !takeShortsParameter)
            {
                throw new ArgumentException("Must take at least longs or shorts");
            }

            if (!Enum.IsDefined(typeof(InitialStopLossRule), initialStopLossRule))
            {
                throw new ArgumentException("Invalid initial stop loss rule");
            }

            if (!Enum.IsDefined(typeof(TrailingStopLossRule), trailingStopLossRule))
            {
                throw new ArgumentException("Invalid trailing stop loss rule");
            }

            if (initialStopLossInPips < 0 || initialStopLossInPips > 999)
            {
                throw new ArgumentException("Invalid initial stop loss - must be between 0 and 999");
            }

            if (trailingStopLossInPips < 0 || trailingStopLossInPips > 999)
            {
                throw new ArgumentException("Invalid trailing stop loss - must be between 0 and 999");
            }

            if (!Enum.IsDefined(typeof(LotSizingRule), lotSizingRule))
            {
                throw new ArgumentException("Invalid lot sizing rule");
            }

            if (takeProfitInPips < 0 || takeProfitInPips > 999)
            {
                throw new ArgumentException("Invalid take profit - must be between 0 and 999");
            }

            if (!Enum.IsDefined(typeof(TakeProfitRule), takeProfitRule))
            {
                throw new ArgumentException("Invalid take profit rule");
            }

            if ((TakeProfitRule)takeProfitRule != TakeProfitRule.StaticPipsValue && takeProfitInPips != 0)
            {
                throw new ArgumentException("Invalid take profit - must be 0 when Take Profit Rule is not Static Pips");
            }

            if (minutesToWaitAfterPositionClosed < 0 || minutesToWaitAfterPositionClosed > 60 * 24)
            {
                throw new ArgumentException(string.Format("Invalid 'Pause after position closed' - must be between 0 and {0}", 60 * 24));
            }

            if (!moveToBreakEven && closeHalfAtBreakEven)
            {
                throw new ArgumentException("'Close half at breakeven?' is only valid when 'Move to breakeven?' is set");
            }

            var lotSizing = (LotSizingRule)lotSizingRule;
            if (lotSizing == LotSizingRule.Dynamic && (dynamicRiskPercentage <= 0 || dynamicRiskPercentage >= 10))
            {
                throw new ArgumentOutOfRangeException("Dynamic Risk value is out of range - it is a percentage (e.g. 2)");
            }
        }

        protected override void OnTick()
        {
            if (_currentPosition == null)
                return;

            ManageExistingPosition();                
        }

        protected override void OnBar()
        {
            if (_currentPosition != null)
            {
                BarsSinceEntry++;
                Print("Bars since entry: {0}", BarsSinceEntry);
            }

            if (!_canOpenPosition || PendingOrders.Any())
                return;

            if (ShouldWaitBeforeLookingForNewSetup())
                return;

            if (_takeLongsParameter && HasBullishSignal())
            {
                EnterLongPosition();
            }
            else if (_takeShortsParameter && HasBearishSignal())
            {
                EnterShortPosition();
            }
        }

        private void ManageExistingPosition()
        {
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    ManageLongPosition();
                    break;

                case TradeType.Sell:
                    ManageShortPosition();
                    break;
            }
        }

        /// <summary>
        /// Manages an existing long position.  Note this method is called on every tick.
        /// </summary>
        protected virtual void ManageLongPosition()
        {
            if (_trailingStopLossRule == TrailingStopLossRule.None && !_moveToBreakEven)
                return;

            // Are we making higher highs?
            var madeNewHigh = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Ask >= BreakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForLongPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);                    
                }

                return;
            }

            if (!ShouldTrail)
            {
                return;
            }

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent high {1}", Symbol.Bid, _recentHigh + buffer);
            if (Symbol.Ask > RecentHigh + buffer && _currentPosition.Pips > 0)
            {
                madeNewHigh = true;
                RecentHigh = Math.Max(Symbol.Ask, MarketSeries.High.Maximum(BarsSinceEntry + 1));
                Print("Recent high set to {0}", RecentHigh);
            }

            if (!madeNewHigh)
            {
                return;
            }

            var stop = CalulateTrailingStopForLongPosition();
            AdjustStopLossForLongPosition(stop);
        }

        private void AdjustStopLossForLongPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value >= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalulateTrailingStopForLongPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRule.StaticPipsValue:
                    stop = Symbol.Ask - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.CurrentBarNPips:
                    stop = MarketSeries.Low.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.PreviousBarNPips:
                    var low = Math.Min(MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
                    stop = low - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.ShortTermHighLow:
                    stop = RecentHigh - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.SmartProfitLocker:    
                    stop = CalculateSmartTrailingStopForLong();                    
                    break;
            }

            return stop;
        }

        /// <summary>
        /// Manages an existing short position.  Note this method is called on every tick.
        /// </summary>
        protected virtual void ManageShortPosition()
        {
            if (_trailingStopLossRule == TrailingStopLossRule.None && !_moveToBreakEven)
                return;

            // Are we making lower lows?
            var madeNewLow = false;

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Bid <= BreakEvenPrice)
            {
                Print("Moving stop loss to entry as we hit breakeven");
                AdjustStopLossForShortPosition(_currentPosition.EntryPrice);
                _alreadyMovedToBreakEven = true;

                if (_closeHalfAtBreakEven)
                {
                    _isClosingHalf = true;
                    ModifyPosition(_currentPosition, _currentPosition.VolumeInUnits / 2);
                }

                return;
            }

            if (!ShouldTrail)
            {
                return;
            }

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent low {1}", Symbol.Bid, _recentLow - buffer);
            if (Symbol.Bid < RecentLow - buffer && _currentPosition.Pips > 0)
            {
                madeNewLow = true;
                RecentLow = Math.Min(Symbol.Bid, MarketSeries.Low.Minimum(BarsSinceEntry + 1));
                Print("Recent low set to {0}", RecentLow);
            }

            if (!madeNewLow)
            {
                return;
            }

            var stop = CalulateTrailingStopForShortPosition();
            AdjustStopLossForShortPosition(stop);
        }

        private void AdjustStopLossForShortPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value <= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalulateTrailingStopForShortPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRule.StaticPipsValue:
                    stop = Symbol.Bid + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.CurrentBarNPips:
                    stop = MarketSeries.High.Last(1) + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.PreviousBarNPips:
                    var high = Math.Max(MarketSeries.High.Last(1), MarketSeries.High.Last(2));
                    stop = high + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.ShortTermHighLow:
                    stop = RecentLow + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.SmartProfitLocker:
                    stop = CalculateSmartTrailingStopForShort();
                    break;
            }

            return stop;
        }

        private double? CalculateSmartTrailingStopForLong()
        {
            var minStop = 20;
            double stop;

            if (_currentPosition.Pips < minStop)
            {
                Print("Band 20");
                stop = minStop;
            }
            else if (_currentPosition.Pips < 40)
            {
                Print("Band 40");
                stop = 16;
            }
            else if (_currentPosition.Pips < 50)
            {
                Print("Band 50");
                stop = 12;
            }
            else
            {
                Print("Band MAX");
                stop = 8;
            }

            stop = RecentHigh - stop * Symbol.PipSize;
            return stop;
        }


        private double? CalculateSmartTrailingStopForShort()
        {
            var minStop = 20;
            double stop;

            if (_currentPosition.Pips < minStop)
            {
                Print("Band 20");
                stop = minStop;
            }
            else if (_currentPosition.Pips < 40)
            {
                Print("Band 40");
                stop = 16;
            }
            else if (_currentPosition.Pips < 50)
            {
                Print("Band 50");
                stop = 12;
            }
            else
            {
                Print("Band MAX");
                stop = 8;
            }

            stop = RecentLow + stop * Symbol.PipSize;
            return stop;
        }

        private bool ShouldWaitBeforeLookingForNewSetup()
        {
            if (_minutesToWaitAfterPositionClosed > 0 &&
                _lastClosedPositionTime != DateTime.MinValue &&
                Server.Time.Subtract(_lastClosedPositionTime).TotalMinutes <= _minutesToWaitAfterPositionClosed)
            {
                Print("Pausing before we look for new opportunities.");
                return true;
            }

            // Alternately, avoid trading on a Friday evening
            var openTime = MarketSeries.OpenTime.LastValue;
            if (openTime.DayOfWeek == DayOfWeek.Friday && openTime.Hour >= 16)
            {
                Print("Avoiding trading on a Friday afternoon");
                return true;
            }

            return false;
        }

        protected virtual void EnterLongPosition()
        {                        
            var stopLossPips = CalculateInitialStopLossInPipsForLongPosition();
            double lots;

            if (stopLossPips.HasValue)
            {
                lots = CalculatePositionQuantityInLots(stopLossPips.Value);
                Print("SL calculated for Buy order = {0}", stopLossPips);                
            }
            else
            {
                lots = 1;
            }

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
            ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
        }

        private double CalculatePositionQuantityInLots(double stopLossPips)
        {
            if (_lotSizingRule == LotSizingRule.Static)
            {
                return 1;
            }
           
            var risk = Account.Equity * _dynamicRiskPercentage / 100;
            var oneLotRisk = Symbol.PipValue * stopLossPips * Symbol.LotSize;
            var quantity = Math.Round(risk / oneLotRisk, 1);

            Print("Account Equity={0}, Risk={1}, Risk for one lot based on SL of {2} = {3}, Qty = {4}",
                Account.Equity, risk, stopLossPips, oneLotRisk, quantity);

            return quantity;
        }

        protected virtual double? CalculateInitialStopLossInPipsForLongPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case InitialStopLossRule.None:
                    break;

                case InitialStopLossRule.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case InitialStopLossRule.CurrentBarNPips:
                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - MarketSeries.Low.Last(1)) / Symbol.PipSize;
                    break;

                case InitialStopLossRule.PreviousBarNPips:
                    var low = MarketSeries.Low.Last(1);
                    if (MarketSeries.Low.Last(2) < low)
                    {
                        low = MarketSeries.Low.Last(2);
                    }

                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - low) / Symbol.PipSize;
                    break;
            }

            if (stopLossPips.HasValue)
            {
                return Math.Round(stopLossPips.Value, 1);
            }

            return null;
        }

        private double? CalculateTakeProfit(double? stopLossPips)
        {
            switch (_takeProfitRule)
            {
                case TakeProfitRule.None:
                    return null;

                case TakeProfitRule.StaticPipsValue:
                    return _takeProfitInPips;

                case TakeProfitRule.DoubleRisk:
                    return stopLossPips.HasValue 
                        ? stopLossPips.Value * 2 
                        : (double?)null;

                case TakeProfitRule.TripleRisk:
                    return stopLossPips.HasValue
                        ? stopLossPips.Value * 3
                        : (double?)null;

                default:
                    return null;
            }
        }

        protected virtual void EnterShortPosition()
        {            
            var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();
            double lots;

            if (stopLossPips.HasValue)
            {
                lots = CalculatePositionQuantityInLots(stopLossPips.Value);
                Print("SL calculated for Sell order = {0}", stopLossPips);                
            }
            else
            {
                lots = 1;
            }

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
            ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
        }

        protected virtual double? CalculateInitialStopLossInPipsForShortPosition()
        {
            double? stopLossPips = null;

            switch (_initialStopLossRule)
            {
                case InitialStopLossRule.None:
                    break;

                case InitialStopLossRule.StaticPipsValue:
                    stopLossPips = _initialStopLossInPips;
                    break;

                case InitialStopLossRule.CurrentBarNPips:
                    stopLossPips = _initialStopLossInPips + (MarketSeries.High.Last(1) - Symbol.Bid) / Symbol.PipSize;
                    break;

                case InitialStopLossRule.PreviousBarNPips:
                    var high = MarketSeries.High.Last(1);
                    if (MarketSeries.High.Last(2) > high)
                    {
                        high = MarketSeries.High.Last(2);
                    }

                    stopLossPips = _initialStopLossInPips + (high - Symbol.Bid) / Symbol.PipSize;
                    break;
            }

            if (stopLossPips.HasValue)
            {
                return Math.Round(stopLossPips.Value, 1);
            }

            return null;

        }

        protected virtual void OnPositionOpened(PositionOpenedEventArgs args)
        {
            BarsSinceEntry = 0;
            RecentHigh = InitialRecentHigh;
            RecentLow = InitialRecentLow;
            _currentPosition = args.Position;
            var position = args.Position;
            var sl = position.StopLoss.HasValue
                ? string.Format(" (SL={0})", position.StopLoss.Value)
                : string.Empty;

            var tp = position.TakeProfit.HasValue
                ? string.Format(" (TP={0})", position.TakeProfit.Value)
                : string.Empty;

            Print("{0} {1:N} at {2}{3}{4}", position.TradeType, position.VolumeInUnits, position.EntryPrice, sl, tp);

            CalculateBreakEvenPrice();
            CalculateDoubleRiskPrice();
            CalculateTripleRiskPrice();
            CalculateTrailingInitiationPrice();

            _canOpenPosition = false;
            ShouldTrail = true;
        }

        private void CalculateBreakEvenPrice()
        {
            //Print("Current position's SL = {0}", _currentPosition.StopLoss.HasValue
            //    ? _currentPosition.StopLoss.Value.ToString()
            //    : "N/A");
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        BreakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
                    }
                    
                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        BreakEvenPrice = Symbol.Bid * 2 - _currentPosition.StopLoss.Value;
                    }

                    break;
            }
        }

        private void CalculateDoubleRiskPrice()
        {
            // Don't bother if we're never going to use it
            if (_takeProfitRule == TakeProfitRule.DoubleRisk)
            {
                DoubleRiskPrice = CalculateRiskPrice(2);
            }
        }

        private void CalculateTripleRiskPrice()
        {
            // Don't bother if we're never going to use it
            if (_takeProfitRule == TakeProfitRule.TripleRisk)
            {
                TripleRiskPrice = CalculateRiskPrice(3);
            }
        }

        private void CalculateTrailingInitiationPrice()
        {
            TrailingInitiationPrice = CalculateRiskPrice(0.75);
        }

        private double CalculateRiskPrice(double multiplier)
        {
            double diff;
            switch (_currentPosition.TradeType)
            {
                case TradeType.Buy:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        diff = _currentPosition.EntryPrice - _currentPosition.StopLoss.Value;
                        return _currentPosition.EntryPrice + (diff * multiplier);
                    }

                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        diff = _currentPosition.StopLoss.Value - _currentPosition.EntryPrice;
                        return _currentPosition.EntryPrice - (diff * multiplier);
                    }

                    break;
            }

            return 0;
        }

        protected virtual void OnPositionClosed(PositionClosedEventArgs args)
        {
            _currentPosition = null;            
            _alreadyMovedToBreakEven = false;

            ExitPrice = CalculateExitPrice(args.Position);
            PrintClosedPositionInfo(args.Position);

            _lastClosedPositionTime = Server.Time;

            _canOpenPosition = true;
        }

        private void OnPositionModified(PositionModifiedEventArgs args)
        {
            if (!_isClosingHalf)
                return;

            ExitPrice = CalculateExitPrice(args.Position);
            PrintClosedPositionInfo(args.Position);
            _isClosingHalf = false;
        }

        private void PrintClosedPositionInfo(Position position)
        {
            Print("Closed {0:N} {1} at {2} for {3} profit (pips={4})",
                position.VolumeInUnits, position.TradeType, ExitPrice, position.GrossProfit, position.Pips);            
        }

        private double CalculateExitPrice(Position position)
        {
            var diff = position.Pips * Symbol.PipSize;
            double exitPrice;
            if (position.TradeType == TradeType.Buy)
            {
                exitPrice = position.EntryPrice + diff;
            }
            else
            {
                exitPrice = position.EntryPrice - diff;
            }

            return 0;
        }
    }

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


