using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;

namespace cAlgo.Library.Robots.VectorVestDowBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class VectorVestDowBot : BaseRobot
    {
        const string ConnectionString = @"Data Source = (localdb)\MSSQLLocalDB; Initial Catalog = cTrader; Integrated Security = True; Connect Timeout = 10; Encrypt = False;";

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

        protected override void OnStart()
        {
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
                0,
                0);

            Notifications.SendEmail("rechambers11@gmail.com", "rechambers11@gmail.com", "MA Cross Over robot initialized", "This is a test");

            if (RecordSession)
            {
                _runId = SaveRunToDatabase();
                if (_runId <= 0)
                    throw new InvalidOperationException("Run Id was <= 0!");
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

        protected override void EnterLongPosition()
        {
            //var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();
            //double lots;

            //if (stopLossPips.HasValue)
            //{
            //    lots = CalculatePositionQuantityInLots(stopLossPips.Value);
            //    Print("SL calculated for Sell order = {0}", stopLossPips);
            //}
            //else
            //{
            //    lots = 1;
            //}

            //var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
            //ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));

            base.EnterLongPosition();
        }

        protected override void EnterShortPosition()
        {
            //var stopLossPips = CalculateInitialStopLossInPipsForShortPosition();
            //double lots;

            //if (stopLossPips.HasValue)
            //{
            //    lots = CalculatePositionQuantityInLots(stopLossPips.Value);
            //    Print("SL calculated for Sell order = {0}", stopLossPips);
            //}
            //else
            //{
            //    lots = 1;
            //}

            //var volumeInUnits = Symbol.QuantityToVolumeInUnits(lots);
            //ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));

            base.EnterShortPosition();
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
            // Assume we are back-testing with dates indicating when we get the DEW Up signal

            return true;


            //var value = _maCrossIndicator.UpSignal.Last(1);
            //if (!value.Equals(double.NaN))
            //    return true;

            //if (!AreMovingAveragesStackedBullishly())
            //{
            //    return false;
            //}

            //var lastCross = GetLastBullishBowtie();
            //if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
            //{
            //    // Either there was no cross or it was too long ago and we have missed the move
            //    return false;
            //}

            //Print("Bullish cross identified at index {0}", lastCross);

            //if (MarketSeries.Close.LastValue <= _fastMA.Result.LastValue)
            //{
            //    //Print("Setup rejected as we closed lower than the fast MA");
            //    return false;
            //}

            //return true;

            return false;
        }

        private bool MAsShouldAreFlatForBullishSetup()
        {
            var index = 1;
            var lowIndex = 1;
            var low = double.MaxValue;

            while (index <= 40)
            {
                if (MarketSeries.Low.Last(index) < low)
                {
                    low = MarketSeries.Low.Last(index);
                    lowIndex = index;
                }

                index++;
            }

            var distance = (_fastMA.Result.Last(lowIndex) - low) / Symbol.PipSize;
            Print("Distance from low to fast MA: {0}", distance);

            return distance <= 46;
        }

        private bool MAsShouldAreFlatForBearishSetup()
        {
            var index = 1;
            var highIndex = 1;
            var high = 0.0;

            while (index <= 40)
            {
                if (MarketSeries.High.Last(index) > high)
                {
                    high = MarketSeries.High.Last(index);
                    highIndex = index;
                }

                index++;
            }

            var distance = (high - _fastMA.Result.Last(highIndex)) / Symbol.PipSize;
            Print("Distance from high to fast MA: {0}", distance);

            return distance <= 46;
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
                    throw new InvalidOperationException("Position ID was <= 0!");
            }
        }

        protected override void OnPositionClosed(PositionClosedEventArgs args)
        {
            base.OnPositionClosed(args);

            if (RecordSession)
                SaveClosedPositionToDatabase(args.Position);
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

        private bool AreMovingAveragesStackedBullishly()
        {
            return _fastMA.Result.LastValue > _mediumMA.Result.LastValue &&
                _mediumMA.Result.LastValue > _slowMA.Result.LastValue;
        }

        private bool AreMovingAveragesStackedBearishly()
        {
            return _fastMA.Result.LastValue < _mediumMA.Result.LastValue &&
                _mediumMA.Result.LastValue < _slowMA.Result.LastValue;
        }

        private bool AreMovingAveragesStackedBullishlyAtIndex(int index)
        {
            return _fastMA.Result.Last(index) > _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) > _slowMA.Result.Last(index);
        }

        private bool AreMovingAveragesStackedBearishlyAtIndex(int index)
        {
            return _fastMA.Result.Last(index) < _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) < _slowMA.Result.Last(index);
        }

        private int GetLastBullishBowtie()
        {
            if (!AreMovingAveragesStackedBullishly())
                return -1;

            var index = 1;
            while (index <= 40)
            {
                if (AreMovingAveragesStackedBullishlyAtIndex(index))
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }

            return -1;
        }

        private int GetLastBearishBowtie()
        {
            if (!AreMovingAveragesStackedBearishly())
                return -1;

            var index = 1;
            while (index <= 30)
            {
                if (AreMovingAveragesStackedBearishlyAtIndex(index))
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }

            return -1;
        }

        protected override bool HasBearishSignal()
        {
            var value = _maCrossIndicator.DownSignal.Last(1);
            if (!value.Equals(double.NaN))
            {
                return true;
            }

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

            //return true;
            return false;
        }

        protected override bool ManageLongPosition()
        {
            //double value;
            //string maType;

            //switch (_maCrossRule)
            //{
            //    case MaCrossRuleValues.CloseOnFastMaCross:
            //        value = _fastMA.Result.LastValue;
            //        maType = "fast";
            //        break;

            //    case MaCrossRuleValues.CloseOnMediumMaCross:
            //        value = _mediumMA.Result.LastValue;
            //        maType = "medium";
            //        break;

            //    default:
            //        return true;
            //}

            // Check if RSI is extended
            if (_rsi.Result.LastValue >= 80)
            {
                Print("Closing position now that the RSI is extended");
                _canOpenPosition = true;
                _currentPosition.Close();
                return true;
            }

            //if (MarketSeries.Close.Last(1) < value - 2 * Symbol.PipSize)
            //{
            //    Print("Closing position now that we closed below the {0} MA", maType);
            //    _canOpenPosition = true;
            //    _currentPosition.Close();
            //}

            return true;
        }

        protected override bool ManageShortPosition()
        {
            if (!ShouldTrail && Symbol.Bid < TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop lower
            if (!base.ManageShortPosition())
                return false;

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case MaCrossRuleValues.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case MaCrossRuleValues.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                default:
                    return true;
            }

            if (MarketSeries.Close.Last(1) > value + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the {0} MA", maType);
                _currentPosition.Close();
            }

            return true;
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


