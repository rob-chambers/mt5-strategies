﻿using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;

namespace cAlgo.Library.Robots.WaveCatcher
{
    public enum InitialStopLossRule
    {
        None,
        CurrentBarNPips,
        PreviousBarNPips,
        StaticPipsValue
    };

    public enum TrailingStopLossRule
    {
        None,        
        CurrentBarNPips,
        PreviousBarNPips,
        ShortTermHighLow,
        StaticPipsValue
    };

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
    public class WaveCatcherBot : BaseRobot
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

        [Parameter("Take Profit (pips)", DefaultValue = 60)]
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

        [Parameter("MAs Cross Threshold (# bars)", DefaultValue = 10)]
        public int MovingAveragesCrossThreshold { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = 1)]
        public int MaCrossRule { get; set; }

        [Parameter("Record", DefaultValue = false)]
        public bool RecordSession { get; set; }

        protected override string Name
        {
            get
            {
                return "WaveCatcher";
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
            Print("Take profit in pips: {0}", TakeProfitInPips);
            Print("Minutes to wait after position closed: {0}", MinutesToWaitAfterPositionClosed);
            Print("Move to breakeven: {0}", MoveToBreakEven);
            Print("Close half at breakeven: {0}", CloseHalfAtBreakEven);
            Print("MA Cross Rule: {0}", MaCrossRule);
            Print("H4MA: {0}", H4MaPeriodParameter);
            Print("Recording: {0}", RecordSession);
            _maCrossRule = (MaCrossRule)MaCrossRule;

            Init(TakeLongsParameter, 
                TakeShortsParameter,
                InitialStopLossRule,
                InitialStopLossInPips,
                TrailingStopLossRule,
                TrailingStopLossInPips,
                LotSizingRule,
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

            if (_maCrossRule == WaveCatcher.MaCrossRule.None && slRule == WaveCatcher.InitialStopLossRule.None && rule == WaveCatcher.TrailingStopLossRule.None)
            {
                throw new ArgumentException("The combination of parameters means that a position may incur a massive loss");
            }

            if (H4MaPeriodParameter < 10 || H4MaPeriodParameter > 99)
            {
                throw new ArgumentException("H4 MA Period must be between 10 and 99");
            }
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
            /* RULES
            1) Fast MA > Medium MA > Slow MA (MAs are 'stacked')
            2) Crossing of MAs must have occurred in the last n bars
            3) Close > Fast MA
            4) Current Close > Prior close
            5) Close > Open
            6) Current High > Prior high
             */
            if (AreMovingAveragesStackedBullishly())
            {
                var lastCross = GetLastBullishBowtie();
                if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
                {
                    // Either there was no cross or it was too long ago and we have missed the move
                    return false;
                }

                Print("Bullish cross identified at index {0}", lastCross);

                if (MarketSeries.Close.LastValue <= _fastMA.Result.LastValue)
                {
                    //Print("Setup rejected as we closed lower than the fast MA");
                    return false;
                }

                if (MarketSeries.Close.Last(1) <= MarketSeries.Close.Last(2))
                {
                    //Print("Setup rejected as we closed lower than the prior close ({0} vs {1})",
                    //    MarketSeries.Close.Last(1), MarketSeries.Close.Last(2));
                    return false;
                }

                if (MarketSeries.Close.Last(1) <= MarketSeries.Open.Last(1))
                {
                    //Print("Setup rejected as we closed lower than the open ({0} vs {1})",
                    //    MarketSeries.Close.Last(1), MarketSeries.Open.Last(1));
                    return false;
                }

                if (MarketSeries.High.Last(1) <= MarketSeries.High.Last(2))
                {
                    //Print("Setup rejected as the high wasn't higher than the prior high ({0} vs {1})",
                    //    MarketSeries.High.Last(1), MarketSeries.High.Last(2));
                    return false;
                }

                return true;
            }

            return false;
        }

        protected override void OnPositionOpened(PositionOpenedEventArgs args)
        {
            base.OnPositionOpened(args);

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
            return _fastMA.Result.Last(index) > _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) > _slowMA.Result.Last(index);
        }

        private int GetLastBullishBowtie()
        {
            if (!AreMovingAveragesStackedBullishly())
            {
                return -1;
            }

            var index = 1;
            while (index <= 30)
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
            {
                return -1;
            }

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
            /* RULES
            1) Fast MA < Medium MA < Slow MA (MAs are 'stacked')
            2) Crossing of MAs must have occurred in the last n bars
            3) Close < Fast MA
            4) Close < Prior close
            5) Close < Open
            6) Low < Prior low
             */
            if (AreMovingAveragesStackedBearishly())
            {
                var lastCross = GetLastBearishBowtie();
                if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
                {
                    // Either there was no cross or it was too long ago and we have missed the move
                    return false;
                }

                Print("Bearish cross identified at index {0}", lastCross);

                if (MarketSeries.Close.LastValue >= _fastMA.Result.LastValue)
                {
                    //Print("Setup rejected as we closed higher than the fast MA");
                    return false;
                }

                if (MarketSeries.Close.Last(1) >= MarketSeries.Close.Last(2))
                {
                    //Print("Setup rejected as we closed higher than the prior close ({0} vs {1})",
                    //    MarketSeries.Close.Last(1), MarketSeries.Close.Last(2));
                    return false;
                }

                if (MarketSeries.Close.Last(1) >= MarketSeries.Open.Last(1))
                {
                    //Print("Setup rejected as we closed higher than the open ({0} vs {1})",
                    //    MarketSeries.Close.Last(1), MarketSeries.Open.Last(1));
                    return false;
                }

                if (MarketSeries.Low.Last(1) >= MarketSeries.Low.Last(2))
                {
                    //Print("Setup rejected as the low wasn't lower than the prior low ({0} vs {1})",
                    //    MarketSeries.Low.Last(1), MarketSeries.Low.Last(2));
                    return false;
                }

                return true;
            }

            return false;
        }

        protected override void ManageLongPosition()
        {
            // Important - call base functionality to trail stop higher
            base.ManageLongPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case WaveCatcher.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case WaveCatcher.MaCrossRule.CloseOnMediumMaCross:
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
            // Important - call base functionality to trail stop lower
            base.ManageShortPosition();

            double value;
            string maType;

            switch (_maCrossRule)
            {
                case WaveCatcher.MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                case WaveCatcher.MaCrossRule.CloseOnMediumMaCross:
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
        private const int _initialRecentLow = int.MaxValue;

        protected abstract string Name { get; }
        protected Position _currentPosition;
        protected double ExitPrice { get; private set; }

        private bool _takeLongsParameter;
        private bool _takeShortsParameter;
        private InitialStopLossRule _initialStopLossRule;
        private TrailingStopLossRule _trailingStopLossRule;
        private LotSizingRule _lotSizingRule;
        private int _initialStopLossInPips;
        private int _takeProfitInPips;
        private int _trailingStopLossInPips;
        private int _minutesToWaitAfterPositionClosed;
        private bool _moveToBreakEven;
        private bool _closeHalfAtBreakEven;
        private double _dynamicRiskPercentage;
        private bool _canOpenPosition;
        private DateTime _lastClosedPositionTime;
        private double _takeProfitLevel;
        private double _recentHigh;
        private bool _inpMoveToBreakEven;
        private bool _alreadyMovedToBreakEven;
        private double _breakEvenPrice;
        private bool _isClosingHalf;
        private double _recentLow;

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
            int takeProfitInPips = 0,            
            int minutesToWaitAfterPositionClosed = 0,
            bool moveToBreakEven = false,
            bool closeHalfAtBreakEven = false,
            double dynamicRiskPercentage = 2)
        {
            ValidateParameters(takeLongsParameter, takeShortsParameter, initialStopLossRule, initialStopLossInPips,
                    trailingStopLossRule, trailingStopLossInPips, lotSizingRule, takeProfitInPips,
                    minutesToWaitAfterPositionClosed, moveToBreakEven, closeHalfAtBreakEven, dynamicRiskPercentage);

            _takeLongsParameter = takeLongsParameter;
            _takeShortsParameter = takeShortsParameter;
            _initialStopLossRule = (InitialStopLossRule)initialStopLossRule;
            _initialStopLossInPips = initialStopLossInPips;
            _trailingStopLossRule = (TrailingStopLossRule)trailingStopLossRule;
            _trailingStopLossInPips = trailingStopLossInPips;
            _lotSizingRule = (LotSizingRule)lotSizingRule;
            _takeProfitInPips = takeProfitInPips;
            _minutesToWaitAfterPositionClosed = minutesToWaitAfterPositionClosed;
            _moveToBreakEven = moveToBreakEven;
            _closeHalfAtBreakEven = closeHalfAtBreakEven;
            _dynamicRiskPercentage = dynamicRiskPercentage;

            _canOpenPosition = true;
            _recentHigh = 0;
            _recentLow = _initialRecentLow;

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
            if (!_canOpenPosition || PendingOrders.Count > 0)
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

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Ask >= _breakEvenPrice)
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

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent high {1}", Symbol.Bid, _recentHigh + buffer);
            if (Symbol.Ask > _recentHigh + buffer)
            {
                madeNewHigh = true;
                _recentHigh = Symbol.Ask;
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
                    stop = _recentHigh - _trailingStopLossInPips * Symbol.PipSize;
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

            if (_moveToBreakEven && !_alreadyMovedToBreakEven && Symbol.Bid <= _breakEvenPrice)
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

            // Avoid adjusting trailing stop too often by adding a buffer
            var buffer = Symbol.PipSize * 3;

            //Print("Comparing current bid price of {0} to recent low {1}", Symbol.Bid, _recentLow - buffer);
            if (Symbol.Bid < _recentLow - buffer)
            {
                madeNewLow = true;
                _recentLow = Symbol.Bid;
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
                    stop = _recentLow + _trailingStopLossInPips * Symbol.PipSize;
                    break;
            }

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

            return false;
        }

        private void EnterLongPosition()
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
            ExecuteMarketOrder(TradeType.Buy, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit());
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

        private double? CalculateInitialStopLossInPipsForLongPosition()
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

        private double? CalculateTakeProfit()
        {
            return _takeProfitInPips == 0 
                ? (double?)null 
                : _takeProfitInPips;
        }

        private void EnterShortPosition()
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
            ExecuteMarketOrder(TradeType.Sell, Symbol, volumeInUnits, Name, stopLossPips, CalculateTakeProfit());
        }

        private double? CalculateInitialStopLossInPipsForShortPosition()
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

            _canOpenPosition = false;            
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
                        _breakEvenPrice = Symbol.Ask * 2 - _currentPosition.StopLoss.Value;
                    }
                    
                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        _breakEvenPrice = Symbol.Bid * 2 - _currentPosition.StopLoss.Value;
                    }

                    break;
            }
        }

        protected virtual void OnPositionClosed(PositionClosedEventArgs args)
        {
            _currentPosition = null;
            _recentHigh = 0;
            _recentLow = _initialRecentLow;
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
}
