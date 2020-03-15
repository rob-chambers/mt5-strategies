using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;
using System.Linq;
// ReSharper disable UseStringInterpolation
// ReSharper disable ArrangeAccessorOwnerBody

namespace cAlgo.Library.Robots.WaveCatcher
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
        CloseOnMediumMaCross,
        CloseOnSlowMaCross
    }

    public enum CustomInitialStopLossRule
    {
        MediumMa,
        SlowMa
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

        [Parameter("Initial SL Rule", DefaultValue = InitialStopLossRule.Custom)]
        public InitialStopLossRule InitialStopLossRule { get; set; }

        [Parameter("Custom Initial SL Rule", DefaultValue = CustomInitialStopLossRule.SlowMa)]
        public CustomInitialStopLossRule CustomInitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = TrailingStopLossRule.None)]
        public TrailingStopLossRule TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = LotSizingRule.Dynamic)]
        public LotSizingRule LotSizingRule { get; set; }

        [Parameter("Take Profit Rule", DefaultValue = TakeProfitRule.None)]
        public TakeProfitRule TakeProfitRule { get; set; }

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

        [Parameter("MA Cross Rule", DefaultValue = MaCrossRule.CloseOnMediumMaCross)]
        public MaCrossRule MaCrossRule { get; set; }

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

        [Parameter("New high/low #bars", DefaultValue = 70)]
        public int NewHighLowBars { get; set; }

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
        private TradeResult _currentTradeResult;
        private double _highestHigh;
        private double _lowestLow;
        private double _lowestLowIndex;

        protected override void OnStart()
        {
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            var h4series = MarketData.GetSeries(Symbol.Name, TimeFrame.Hour4);

            _h4Ma = Indicators.ExponentialMovingAverage(h4series.Close, H4MaPeriodParameter);
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
            InitialStopLossRule initialStopLossRule, 
            int initialStopLossInPips, 
            TrailingStopLossRule trailingStopLossRule, 
            int trailingStopLossInPips, 
            LotSizingRule lotSizingRule, 
            TakeProfitRule takeProfitRule,
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

            var slRule = initialStopLossRule;
            var rule = trailingStopLossRule;

            if (_maCrossRule == MaCrossRule.None && slRule == InitialStopLossRule.None && rule == TrailingStopLossRule.None)
            {
                throw new ArgumentException("The combination of parameters means that a position may incur a massive loss");
            }

            if (H4MaPeriodParameter < 10 || H4MaPeriodParameter > 99)
            {
                throw new ArgumentException("H4 MA Period must be between 10 and 99");
            }
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

            if (EnterAtMarket)
            {
                base.EnterLongPosition();
                return;
            }

            var volumeInUnits = 100000;

            var priorBarRange = Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1);
            priorBarRange /= 4;
            var limitPrice = _fastMA.Result.LastValue + priorBarRange;
            var label = string.Format("BUY {0}", Symbol);
            double? stopLossPips = Math.Round(5 + (limitPrice - _slowMA.Result.LastValue) / Symbol.PipSize, 1);
            var expiry = Server.Time.AddHours(6);

            Print("Placing limit order at {0} with stop {1}", limitPrice, stopLossPips);
            _currentTradeResult = PlaceLimitOrder(TradeType.Buy, Symbol.Name, volumeInUnits, limitPrice, label, stopLossPips, CalculateFibTakeProfit(), expiry);
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

            if (EnterAtMarket)
            {
                base.EnterShortPosition();
                return;
            }

            var volumeInUnits = 100000;
            var priorBarRange = Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1);
            priorBarRange /= 4;
            var limitPrice = _fastMA.Result.LastValue - priorBarRange;
            var label = string.Format("SELL {0}", Symbol);
            double? stopLossPips = Math.Round(5 + (_slowMA.Result.LastValue - limitPrice) / Symbol.PipSize, 1);
            var expiry = Server.Time.AddHours(6);

            Print("Placing limit order at {0} with stop {1}", limitPrice, stopLossPips);
            _currentTradeResult = PlaceLimitOrder(TradeType.Sell, Symbol.Name, volumeInUnits, limitPrice, label, stopLossPips, CalculateFibTakeProfit(), expiry);
        }

        protected override double? CalculateInitialStopLossInPipsForLongPosition()
        {
            if (_initialStopLossRule != InitialStopLossRule.Custom)
            {
                return base.CalculateInitialStopLossInPipsForLongPosition();
            }

            switch (CustomInitialStopLossRule)
            {
                case CustomInitialStopLossRule.MediumMa:
                    var stop = (Symbol.Ask - _mediumMA.Result.LastValue) / Symbol.PipSize;
                    return Math.Round(stop, 1);

                case CustomInitialStopLossRule.SlowMa:
                    stop = (Symbol.Ask - _slowMA.Result.LastValue) / Symbol.PipSize;
                    return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForLongPosition();
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            if (_initialStopLossRule != InitialStopLossRule.Custom)
            {
                return base.CalculateInitialStopLossInPipsForShortPosition();
            }

            switch (CustomInitialStopLossRule)
            {
                case CustomInitialStopLossRule.MediumMa:
                    var stop = (_mediumMA.Result.LastValue - Symbol.Bid) / Symbol.PipSize;
                    return Math.Round(stop, 1);

                case CustomInitialStopLossRule.SlowMa:
                    stop = (_slowMA.Result.LastValue - Symbol.Bid) / Symbol.PipSize;
                    return Math.Round(stop, 1);
            }

            return base.CalculateInitialStopLossInPipsForShortPosition();
        }

        private double? CalculateFibTakeProfit()
        {
            return null;


            // Find highest high from here back
            //var highest = Bars.HighPrices.Maximum(20);
           
            //const double StandardFib = 1.382;

            //var lowestLow = Common.LowestLow(Bars.LowPrices, 10, 6);
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
                    command.Parameters.AddWithValue("@Symbol", Symbol.Name);
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

            if (!AreMovingAveragesStackedBullishly())
            {
                return false;
            }

            var lastCross = GetLastBullishBowtie();
            if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
            {
                // Either there was no cross or it was too long ago and we have missed the move
                return false;
            }

            Print("Bullish cross identified at index {0}", lastCross);

            if (Bars.ClosePrices.Last(1) <= _fastMA.Result.LastValue)
            {
                //Print("Setup rejected as we closed lower than the fast MA");
                return false;
            }

            if (CloseVsPriorCloseFilter && Bars.ClosePrices.Last(1) <= Bars.ClosePrices.Last(2))
            {
                //Print("Setup rejected as we closed lower than the prior close ({0} vs {1})",
                //    Bars.ClosePrices.Last(1), Bars.ClosePrices.Last(2));
                return false;
            }

            if (CloseVsOpenFilter && Bars.ClosePrices.Last(1) <= Bars.OpenPrices.Last(1))
            {
                //Print("Setup rejected as we closed lower than the open ({0} vs {1})",
                //    Bars.ClosePrices.Last(1), Bars.OpenPrices.Last(1));
                return false;
            }

            if (HighLowVsPriorHighLowFilter && Bars.HighPrices.Last(1) <= Bars.HighPrices.Last(2))
            {
                //Print("Setup rejected as the high wasn't higher than the prior high ({0} vs {1})",
                //    Bars.HighPrices.Last(1), Bars.HighPrices.Last(2));
                return false;
            }

            // What's the distance between the MAs?  Avoid noise and ensure there's been a breakout
            if (MADistanceFilter && (_fastMA.Result.LastValue - _mediumMA.Result.LastValue) / Symbol.PipSize <= 3)
            {
                Print("Setup rejected as there wasn't enough distance between the fast and medium MAs");
                return false;
            }

            // What's the distance between the MAs?  Ensure we haven't already missed the move
            if (MAMaxDistanceFilter && (_fastMA.Result.LastValue - _mediumMA.Result.LastValue) / Symbol.PipSize >= 30)
            {
                Print("Setup rejected as the distance between the fast and medium MAs was more than 30 pips");
                return false;
            }

            // How low was the recent lowest low?  Attempt to only enter when the MAs have been flat
            if (MAsFlatFilter && !MAsShouldAreFlatForBullishSetup())
            {
                Print("Setup rejected as the MAs don't seem to be flat");
                return false;
            }

            if (NewHighLowFilter)
            {
                // Another filter - have we hit a new high?
                var high = Bars.HighPrices.Maximum(NewHighLowBars);
                var priorHigh = Bars.HighPrices.Last(1);
                if (Math.Abs(priorHigh - high) > Symbol.PipSize)
                {
                    Print("Setup rejected as the prior high {0} has not gone higher than {1} (pip size = {2})", priorHigh, high, Symbol.PipSize);
                    return false;
                }

                _highestHigh = high;
            }

            return true;
        }

        private bool MAsShouldAreFlatForBullishSetup()
        {
            var index = 1;
            var lowIndex = 1;
            var low = double.MaxValue;

            while (index <= 40)
            {
                if (Bars.LowPrices.Last(index) < low)
                {
                    low = Bars.LowPrices.Last(index);
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
                if (Bars.HighPrices.Last(index) > high)
                {
                    high = Bars.HighPrices.Last(index);
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

                    command.Parameters.AddWithValue("@Open", Bars.OpenPrices.Last(1));
                    command.Parameters.AddWithValue("@High", Bars.HighPrices.Last(1));
                    command.Parameters.AddWithValue("@Low", Bars.LowPrices.Last(1));
                    command.Parameters.AddWithValue("@Close", Bars.ClosePrices.Last(1));
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
            return _fastMA.Result.Last(index) < _mediumMA.Result.Last(index) &&
                _mediumMA.Result.Last(index) < _slowMA.Result.Last(index);
        }

        private int GetLastBullishBowtie()
        {
            if (!AreMovingAveragesStackedBullishly())
            {
                return -1;
            }

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

        private int GetLastBearishBowTie()
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
            if (!AreMovingAveragesStackedBearishly())
            {
                return false;
            }

            var lastCross = GetLastBearishBowTie();
            if (lastCross == -1 || lastCross > MovingAveragesCrossThreshold)
            {
                // Either there was no cross or it was too long ago and we have missed the move
                return false;
            }

            Print("Bearish cross identified at index {0}", lastCross);

            if (Bars.ClosePrices.Last(1) >= _fastMA.Result.LastValue)
            {
                //Print("Setup rejected as we closed higher than the fast MA");
                return false;
            }

            if (CloseVsPriorCloseFilter && Bars.ClosePrices.Last(1) >= Bars.ClosePrices.Last(2))
            {
                //Print("Setup rejected as we closed higher than the prior close ({0} vs {1})",
                //    Bars.ClosePrices.Last(1), Bars.ClosePrices.Last(2));
                return false;
            }

            if (CloseVsOpenFilter && Bars.ClosePrices.Last(1) >= Bars.OpenPrices.Last(1))
            {
                //Print("Setup rejected as we closed higher than the open ({0} vs {1})",
                //    Bars.ClosePrices.Last(1), Bars.OpenPrices.Last(1));
                return false;
            }

            if (HighLowVsPriorHighLowFilter && Bars.LowPrices.Last(1) >= Bars.LowPrices.Last(2))
            {
                //Print("Setup rejected as the low wasn't lower than the prior low ({0} vs {1})",
                //    Bars.LowPrices.Last(1), Bars.LowPrices.Last(2));
                return false;
            }

            // Another filter - what's the distance between the MAs?  Avoid noise and ensure there's been a breakout
            if (MADistanceFilter && (_mediumMA.Result.LastValue - _fastMA.Result.LastValue) / Symbol.PipSize < 7)
            {
                Print("Setup rejected as there wasn't enough distance between the fast and medium MAs");
                return false;
            }

            // What's the distance between the MAs?  Ensure we haven't already missed the move
            if (MAMaxDistanceFilter && (_mediumMA.Result.LastValue - _fastMA.Result.LastValue) / Symbol.PipSize >= 30)
            {
                Print("Setup rejected as the distance between the fast and medium MAs was more than 30 pips");
                return false;
            }

            // How high was the recent highest high?  Attempt to only enter when the MAs have been flat
            if (MAsFlatFilter && !MAsShouldAreFlatForBearishSetup())
            {
                Print("Setup rejected as the MAs don't seem to be flat");
                return false;
            }

            if (NewHighLowFilter)
            {
                // Another filter - have we hit a new low?
                var low = Bars.LowPrices.Minimum(NewHighLowBars);
                var priorLow = Bars.LowPrices.Last(1);
                if (Math.Abs(priorLow - low) > Symbol.PipSize)
                {
                    Print("Setup rejected as the prior low {0} has not gone lower than {1}", priorLow, low);
                    return false;
                }

                _lowestLow = low;
            }

            return true;
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
                case MaCrossRule.CloseOnSlowMaCross:
                    value = _slowMA.Result.LastValue;
                    maType = "slow";
                    break;

                case MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                case MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                default:
                    return;
            }

            if (Symbol.Ask < value - 2 * Symbol.PipSize)
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
                case MaCrossRule.CloseOnSlowMaCross:
                    value = _slowMA.Result.LastValue;
                    maType = "slow";
                    break;

                case MaCrossRule.CloseOnMediumMaCross:
                    value = _mediumMA.Result.LastValue;
                    maType = "medium";
                    break;

                case MaCrossRule.CloseOnFastMaCross:
                    value = _fastMA.Result.LastValue;
                    maType = "fast";
                    break;

                default:
                    return;
            }

            if (Symbol.Bid > value + 2 * Symbol.PipSize)
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
            InitialStopLossRule initialStopLossRule,
            int initialStopLossInPips,
            TrailingStopLossRule trailingStopLossRule,
            int trailingStopLossInPips,
            LotSizingRule lotSizingRule,         
            TakeProfitRule takeProfitRule,
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
            InitialStopLossRule initialStopLossRule, 
            int initialStopLossInPips,
            TrailingStopLossRule trailingStopLossRule,
            int trailingStopLossInPips,
            LotSizingRule lotSizingRule,
            TakeProfitRule takeProfitRule,
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
                throw new ArgumentOutOfRangeException($"Dynamic Risk Percentage", "Dynamic Risk value is out of range - it is a percentage (e.g. 2)");
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
                RecentHigh = Math.Max(Symbol.Ask, Bars.HighPrices.Maximum(BarsSinceEntry + 1));
                Print("Recent high set to {0}", RecentHigh);
            }

            if (!madeNewHigh)
            {
                return;
            }

            var stop = CalculateTrailingStopForLongPosition();
            AdjustStopLossForLongPosition(stop);
        }

        private void AdjustStopLossForLongPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value >= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalculateTrailingStopForLongPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRule.StaticPipsValue:
                    stop = Symbol.Ask - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.CurrentBarNPips:
                    stop = Bars.LowPrices.Last(1) - _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.PreviousBarNPips:
                    var low = Math.Min(Bars.LowPrices.Last(1), Bars.LowPrices.Last(2));
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
                RecentLow = Math.Min(Symbol.Bid, Bars.LowPrices.Minimum(BarsSinceEntry + 1));
                Print("Recent low set to {0}", RecentLow);
            }

            if (!madeNewLow)
            {
                return;
            }

            var stop = CalculateTrailingStopForShortPosition();
            AdjustStopLossForShortPosition(stop);
        }

        private void AdjustStopLossForShortPosition(double? newStop)
        {
            if (!newStop.HasValue || _currentPosition.StopLoss.HasValue && _currentPosition.StopLoss.Value <= newStop.Value)
                return;

            ModifyPosition(_currentPosition, newStop, _currentPosition.TakeProfit);
        }

        private double? CalculateTrailingStopForShortPosition()
        {
            double? stop = null;
            switch (_trailingStopLossRule)
            {
                case TrailingStopLossRule.StaticPipsValue:
                    stop = Symbol.Bid + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.CurrentBarNPips:
                    stop = Bars.HighPrices.Last(1) + _trailingStopLossInPips * Symbol.PipSize;
                    break;

                case TrailingStopLossRule.PreviousBarNPips:
                    var high = Math.Max(Bars.HighPrices.Last(1), Bars.HighPrices.Last(2));
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
            const int minStop = 20;
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
            const int minStop = 20;
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
            ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
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
                    stopLossPips = _initialStopLossInPips + (Symbol.Ask - Bars.LowPrices.Last(1)) / Symbol.PipSize;
                    break;

                case InitialStopLossRule.PreviousBarNPips:
                    var low = Bars.LowPrices.Last(1);
                    if (Bars.LowPrices.Last(2) < low)
                    {
                        low = Bars.LowPrices.Last(2);
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
                    return stopLossPips * 2;

                case TakeProfitRule.TripleRisk:
                    return stopLossPips * 3;

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
            ExecuteMarketOrder(TradeType.Sell, Symbol.Name, volumeInUnits, Name, stopLossPips, CalculateTakeProfit(stopLossPips));
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
                    stopLossPips = _initialStopLossInPips + (Bars.HighPrices.Last(1) - Symbol.Bid) / Symbol.PipSize;
                    break;

                case InitialStopLossRule.PreviousBarNPips:
                    var high = Bars.HighPrices.Last(1);
                    if (Bars.HighPrices.Last(2) > high)
                    {
                        high = Bars.HighPrices.Last(2);
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
                        return _currentPosition.EntryPrice + diff * multiplier;
                    }

                    break;

                case TradeType.Sell:
                    if (_currentPosition.StopLoss.HasValue)
                    {
                        diff = _currentPosition.StopLoss.Value - _currentPosition.EntryPrice;
                        return _currentPosition.EntryPrice - diff * multiplier;
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


