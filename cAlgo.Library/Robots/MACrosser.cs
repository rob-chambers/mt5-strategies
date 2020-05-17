// Version 2020-05-17 20:24
using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Data.SqlClient;
using cAlgo.Library.Indicators;
using Powder.TradingLibrary;

namespace cAlgo.Library.Robots.MACrosser
{
    public enum Confidence
    {
        Low,
        Medium,
        High
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class MACrosserBot : BaseRobot
    {
        private const string ConnectionString = @"Data Source = (localdb)\MSSQLLocalDB; Initial Catalog = cTrader; Integrated Security = True; Connect Timeout = 10; Encrypt = False;";

        #region Standard Parameters
        [Parameter("Take long trades?", DefaultValue = true)]
        public bool TakeLongsParameter { get; set; }

        [Parameter("Take short trades?", DefaultValue = false)]
        public bool TakeShortsParameter { get; set; }

        [Parameter("Initial SL Rule", DefaultValue = 4)]
        public InitialStopLossRuleValues InitialStopLossRule { get; set; }

        [Parameter("Initial SL (pips)", DefaultValue = 5)]
        public int InitialStopLossInPips { get; set; }

        [Parameter("Trailing SL Rule", DefaultValue = 0)]
        public TrailingStopLossRuleValues TrailingStopLossRule { get; set; }

        [Parameter("Trailing SL (pips)", DefaultValue = 10)]
        public int TrailingStopLossInPips { get; set; }

        [Parameter("Lot Sizing Rule", DefaultValue = 0)]
        public LotSizingRuleValues LotSizingRule { get; set; }

        [Parameter("Take Profit Rule", DefaultValue = 0)]
        public TakeProfitRuleValues TakeProfitRule { get; set; }

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

        [Parameter("Bars for trade development", DefaultValue = 3)]
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

        [Parameter("H4 MA Period", DefaultValue = 21)]
        public int H4MaPeriodParameter { get; set; }

        [Parameter("MAs Cross Threshold (# bars)", DefaultValue = 30)]
        public int MovingAveragesCrossThreshold { get; set; }

        [Parameter("MA Cross Rule", DefaultValue = 2)]
        public MaCrossRuleValues MaCrossRule { get; set; }

        [Parameter("Record", DefaultValue = false)]
        public bool RecordSession { get; set; }

        [Parameter("Enter at Market", DefaultValue = true)]
        public bool EnterAtMarket { get; set; }

        protected override string Name
        {
            get
            {
                return "MACrosser";
            }
        }

        private MACrossOver _maCrossIndicator;
        private MovingAverage _fastMA;
        private MovingAverage _mediumMA;
        private MovingAverage _slowMA;        
        private int _runId;
        private int _currentPositionId;
        private RelativeStrengthIndex _rsi;
        private AverageTrueRange _atr;

        private TradeResult _currentTradeResult;
        private Confidence _confidence;

        protected override void OnStart()
        {
            _maCrossIndicator = Indicators.GetIndicator<MACrossOver>(SourceSeries, SlowPeriodParameter, MediumPeriodParameter, FastPeriodParameter, false, false, false);
            _fastMA = Indicators.MovingAverage(SourceSeries, FastPeriodParameter, MovingAverageType.Exponential);
            _mediumMA = Indicators.MovingAverage(SourceSeries, MediumPeriodParameter, MovingAverageType.Exponential);
            _slowMA = Indicators.MovingAverage(SourceSeries, SlowPeriodParameter, MovingAverageType.Exponential);
            _rsi = Indicators.RelativeStrengthIndex(SourceSeries, 14);
            _atr = Indicators.AverageTrueRange(Bars, 14, MovingAverageType.Exponential);

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
            Print("BarsToAllowTradeToDevelop: {0}", BarsToAllowTradeToDevelop);            

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
                DynamicRiskPercentage,
                BarsToAllowTradeToDevelop);

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
            InitialStopLossRuleValues initialStopLossRule, 
            int initialStopLossInPips, 
            TrailingStopLossRuleValues trailingStopLossRule, 
            int trailingStopLossInPips, 
            LotSizingRuleValues lotSizingRule, 
            TakeProfitRuleValues takeProfitRule,
            int takeProfitInPips, 
            int minutesToWaitAfterPositionClosed, 
            bool moveToBreakEven, 
            bool closeHalfAtBreakEven,
            double dynamicRiskPercentage,
            int barsToAllowTradeToDevelop)
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
                barsToAllowTradeToDevelop);

            if (FastPeriodParameter <= 0 || FastPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Fast MA Period' - must be between 1 and 999");

            if (MediumPeriodParameter <= 0 || MediumPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Medium MA Period' - must be between 1 and 999");

            if (SlowPeriodParameter <= 0 || SlowPeriodParameter > 999)
                throw new ArgumentException("Invalid 'Slow MA Period' - must be between 1 and 999");

            if (!(FastPeriodParameter < MediumPeriodParameter && MediumPeriodParameter < SlowPeriodParameter))
                throw new ArgumentException("Invalid 'MA Periods' - fast must be less than medium and medium must be less than slow");

            if (MovingAveragesCrossThreshold <= 0 || MovingAveragesCrossThreshold > 999)
                throw new ArgumentException("MAs Cross Threshold - must be between 1 and 999");

            if (MaCrossRule == MaCrossRuleValues.None && 
                initialStopLossRule == InitialStopLossRuleValues.None && 
                trailingStopLossRule == TrailingStopLossRuleValues.None)
                throw new ArgumentException("The combination of parameters means that a position may incur a massive loss");

            if (H4MaPeriodParameter < 10 || H4MaPeriodParameter > 99)
                throw new ArgumentException("H4 MA Period must be between 10 and 99");
        }

        protected override void EnterLongPosition()
        {
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
            if (_initialStopLossRule != InitialStopLossRuleValues.Custom)
            {
                return base.CalculateInitialStopLossInPipsForLongPosition();
            }

            var stop = GetSmartStopForLong(Symbol.Ask);
            CalculateConfidence(Symbol.Ask);
            return Math.Round(stop, 1);
        }

        private double GetSmartStopForLong(double price)
        {
            var threshold = price - _atr.Result.LastValue * 2;
            var margin = 2 * Symbol.PipSize;
            var minStop = price - 6 * Symbol.PipSize;
            var stop = double.NaN;

            Print("Threshold: {0}", threshold);

            // Keep going back until we find a bar that is far enough away from the price
            for (var i = 2; i < 20; i++)
            {
                var low = Bars.LowPrices.Last(i);
                if (low < threshold)
                {
                    Print("low={0}, index={1}, price={2}", low, i, price);
                    stop = low - margin;
                    break;
                }
            }

            if (double.IsNaN(stop))
            {
                // Really? - Must be very flat - use an ATR stop
                stop = 2 * _atr.Result.LastValue;
            }

            stop = Math.Min(minStop, stop);

            // Calculate actual difference between this stop price and price to get pips
            stop = Symbol.Ask - stop;
            return stop / Symbol.PipSize;
        }

        protected override double? CalculateInitialStopLossInPipsForShortPosition()
        {
            if (_initialStopLossRule != InitialStopLossRuleValues.Custom)
            {
                return base.CalculateInitialStopLossInPipsForShortPosition();
            }

            var stop = GetSmartStopForShort(Symbol.Bid);
            CalculateConfidence(Symbol.Bid);
            return Math.Round(stop, 1);
        }

        private double GetSmartStopForShort(double price)
        {
            var threshold = price + _atr.Result.LastValue * 2;
            var margin = 2 * Symbol.PipSize;
            var minStop = price + 6 * Symbol.PipSize;
            var stop = double.NaN;

            Print("Threshold: {0}", threshold);

            // Keep going back until we find a bar that is far enough away from the price
            for (var i = 2; i < 20; i++)
            {
                var high = Bars.HighPrices.Last(i);
                if (high > threshold)
                {
                    Print("high={0}, index={1}, price={2}", high, i, price);
                    stop = high + margin;
                    break;
                }
            }

            if (double.IsNaN(stop))
            {
                // Really? - Must be very flat - use an ATR stop
                stop = 2 * _atr.Result.LastValue;
            }

            stop = Math.Max(minStop, stop);

            // Calculate actual difference between this stop price and price to get pips
            stop -= Symbol.Bid;
            return stop / Symbol.PipSize;
        }

        private void CalculateConfidence(double price)
        {
            var diff = Math.Abs(price - _mediumMA.Result.LastValue);

            var distanceMultiple = diff / _atr.Result.LastValue;

            var maDiff = Math.Abs(_mediumMA.Result.LastValue - _fastMA.Result.LastValue);
            maDiff /= _atr.Result.LastValue;

            if (distanceMultiple > 3)
            {
                _confidence = Confidence.Low;
            }
            else if (distanceMultiple > 2)
            {
                if (maDiff < 0.9)
                {
                    _confidence = Confidence.High;
                }
                else
                {
                    _confidence = Confidence.Medium;
                }                
            }
            else
            {
                _confidence = Confidence.High;
            }

            Print("Confidence={0}, Distance={1}, MADiff={2}", _confidence, distanceMultiple, maDiff);
        }

        //protected override double? CalculateTakeProfit(double? stopLossPips)
        //{
        //    switch (_confidence)
        //    {
        //        case Confidence.Low:
        //            return stopLossPips.HasValue
        //                ? stopLossPips.Value
        //                : (double?)null;

        //        case Confidence.Medium:
        //            return stopLossPips.HasValue
        //                ? stopLossPips.Value * 2
        //                : (double?)null;

        //        default:
        //            return stopLossPips.HasValue
        //                ? stopLossPips.Value * 3
        //                : (double?)null;
        //    }
        //}

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
                    command.Parameters.AddWithValue("@Symbol", SymbolName);
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
            var value = _maCrossIndicator.UpSignal.Last(1);
            if (value.Equals(double.NaN))
            {
                return false;
            }

            // What's the distance between the MAs?  Avoid noise and ensure there's been a breakout
            //var distance = _fastMA.Result.LastValue - _slowMA.Result.LastValue;
            //var distanceInPips = distance / Symbol.PipSize;
            //Print("Distance: {0}", distanceInPips);

            //distance /= _atr.Result.LastValue;
            //Print("Ratio: {0}", distance);

            //// ratio of 0.74 - 0.82

            //if (distance <= 0.4 || distanceInPips <= 1.4)
            //{
            //    Print("Setup rejected as there wasn't enough distance between the fast and slow MAs");
            //    return false;
            //}

            //// What's the distance between price and the slow MA?  Perhaps we missed the move
            //distance = Symbol.Ask - _slowMA.Result.LastValue;
            //distance /= _atr.Result.LastValue;

            //Print("Price ratio to slow MA: {0}", distance);
            //if (distance >= 3.5)
            //{
            //    Print("Setup rejected as it looks like we have missed the move");
            //    return false;
            //}

            //// What's the distance between the fast and medium MAs?  Avoid noise and ensure there's been a breakout
            //distance = _fastMA.Result.LastValue - _mediumMA.Result.LastValue;
            //distanceInPips = distance / Symbol.PipSize;
            //if (distanceInPips < 1)
            //{
            //    Print("Setup rejected as there wasn't enough distance between the fast and medium MAs: {0}", distanceInPips);
            //    return false;
            //}

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
                    throw new InvalidOperationException("Position ID was <= 0!");
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
                    //command.Parameters.AddWithValue("@H4MA", _h4Ma.Result.LastValue);
                    //command.Parameters.AddWithValue("@H4RSI", _h4Rsi.Result.LastValue);

                    connection.Open();
                    identity = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }

            return identity;
        }

        protected override bool HasBearishSignal()
        {
            var value = _maCrossIndicator.DownSignal.Last(1);
            if (value.Equals(double.NaN))
            {
                return false;
            }

            // What's the distance between the MAs?  Avoid noise and ensure there's been a breakout
            //var distance = _slowMA.Result.LastValue - _fastMA.Result.LastValue;
            //var distanceInPips = distance / Symbol.PipSize;
            //Print("Distance: {0}", distanceInPips);

            //distance /= _atr.Result.LastValue;
            //Print("Ratio: {0}", distance);

            //// ratio of 0.74 - 0.82

            //if (distance <= 0.4 || distanceInPips <= 1.4)
            //{
            //    Print("Setup rejected as there wasn't enough distance between the fast and slow MAs");
            //    return false;
            //}

            //// What's the distance between price and the slow MA?  Perhaps we missed the move
            //distance = _slowMA.Result.LastValue - Symbol.Bid;
            //distance /= _atr.Result.LastValue;

            //Print("Price ratio to slow MA: {0}", distance);
            //if (distance >= 3.5)
            //{
            //    Print("Setup rejected as it looks like we have missed the move");
            //    return false;
            //}

            //// What's the distance between the fast and medium MAs?  Avoid noise and ensure there's been a breakout
            //distance = _mediumMA.Result.LastValue - _fastMA.Result.LastValue;
            //distanceInPips = distance / Symbol.PipSize;
            //if (distanceInPips < 1)
            //{
            //    Print("Setup rejected as there wasn't enough distance between the fast and medium MAs: {0}", distanceInPips);
            //    return false;
            //}

            return true;
        }

        protected override bool ManageLongPosition()
        {
            if (BarsSinceEntry <= BarsToAllowTradeToDevelop)
                return false;

            if (!ShouldTrail && Symbol.Ask > TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop higher
            if (!base.ManageLongPosition()) return false;

            double value;
            string maType;

            switch (MaCrossRule)
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

            if (Bars.ClosePrices.Last(1) < value - 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed below the {0} MA", maType);
                _canOpenPosition = true;
                _currentPosition.Close();
            }

            return true;
        }

        protected override bool ManageShortPosition()
        {
            if (BarsSinceEntry <= BarsToAllowTradeToDevelop)
                return false;

            if (!ShouldTrail && Symbol.Bid < TrailingInitiationPrice)
            {
                ShouldTrail = true;
                Print("Initiating trailing now that we have reached trailing initiation price");
            }

            // Important - call base functionality to trail stop lower
            if (!base.ManageShortPosition()) return false;

            double value;
            string maType;

            switch (MaCrossRule)
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

            if (Bars.ClosePrices.Last(1) > value + 2 * Symbol.PipSize)
            {
                Print("Closing position now that we closed above the {0} MA", maType);
                _currentPosition.Close();
            }

            return true;
        }
    }
}


