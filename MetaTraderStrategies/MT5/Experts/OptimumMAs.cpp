//+------------------------------------------------------------------+
//|                                                     optimumMAs.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.00"
//+------------------------------------------------------------------+
//|VERSION HISTORY
//|v1.00 - Added initial entry rules
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh> 
#include <Trade\PositionInfo.mqh>
#include <Expert\Money\MoneyFixedRisk.mqh>

enum STOPLOSS_RULE
{
    None,
    StaticPipsValue,
    CurrentBar2Pips,
    PreviousBar5Pips,
    PreviousBar2Pips,
    CurrentBar5Pips,
    CurrentBarNPips,
    MediumTermMA,
    LongTermMA
};

//+------------------------------------------------------------------------------------------------------------------------------+
//| Input variables                                                                                                              |
//+------------------------------------------------------------------------------------------------------------------------------+
input double        _inpDynamicSizingRiskPerTrade = 2;              // Risk per trade of account balance
input STOPLOSS_RULE _inpInitialStopLossRule = LongTermMA;           // Initial Stop Loss Rule
input int           _inpInitialStopLossPips = 5;                    // Initial stop loss in pips
input int           _inpTrailingStopLossPips = 5;                   // Trailing stop loss in pips

// Go Long / short parameters
input bool      _inpGoLong = true;                                  // Whether to enter long trades or not
input bool      _inpGoShort = false;                                // Whether to enter short trades or not

// Alert parameters
input bool      _inpAlertTerminalEnabled = true;                    // Whether to show terminal alerts or not
input bool      _inpAlertEmailEnabled = false;                      // Whether to alert via email or not

// Trading time parameters
input int       _inpMinTradingHour = 0;                             // The minimum hour of the day to trade (e.g. 7 for 7am)
input int       _inpMaxTradingHour = 0;                             // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Technical parameters
input int       _inpH4MAPeriod = 21;                                // The H4 timeframe MA period
input int       _inpLongTermPeriod = 89;                            // The long term MA period
input int       _inpMediumTermPeriod = 55;                          // The medium term MA period
input int       _inpShortTermPeriod = 21;                           // The short term MA period
input int       _inpHighestHighNumBars = 15;                        // Period over which we have the highest high
input int       _inpSweetSpot1Lower = 4;
input int       _inpSweetSpot1Upper = 8;
input int       _inpSweetSpot2Lower = 2;
input int       _inpSweetSpot2Upper = 6;

//+------------------------------------------------------------------------------------------------------------------------------+
//| Private variables                                                                                                            |
//+------------------------------------------------------------------------------------------------------------------------------+

int _longTermTimeFrameHandle;
int _veryLongTermTimeFrameHandle;

int _longTermTrendHandle;
int _mediumTermTrendHandle;
int _shortTermTrendHandle;

double _longTermTimeFrameData[];
double _veryLongTermTimeFrameData[];
double _longTermTrendData[];
double _mediumTermTrendData[];
double _shortTermTrendData[];
long _accountMarginMode;
bool _trailedThisBar;

//+------------------------------------------------------------------------------------------------------------------------------+
//| Variables from base class                                                                                                    |
//+------------------------------------------------------------------------------------------------------------------------------+

// Protected
CSymbolInfo _symbol;
CPositionInfo _position;
CTrade _trade;
MqlRates _prices[];
int _digits_adjust;
double _adjustedPoints;
double _currentBid, _currentAsk;
bool _alreadyMovedToBreakEven;
double _initialStop;

// Private
datetime _barTime;                  // For detection of a new bar
double _recentHigh;                 // Tracking the most recent high for stop management
int _barsSincePositionOpened;       // Counter of the number of bars since a position was opened
int _barsSincePositionClosed;       // Counter of the number of bars since a position was closed
int _eventCount;                    // Counter for OnTrade event
int _currentPositionType;           // The current type of position (long/short)
CMoneyFixedRisk _fixedRisk;         // Fixed risk money management class
bool _isNewBar;                     // A flag to indicate if this tick is the start of a new bar

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    int retCode = InitFromBase(_inpDynamicSizingRiskPerTrade, _inpInitialStopLossRule, _inpInitialStopLossPips, _inpGoLong, _inpGoShort, _inpAlertTerminalEnabled, _inpAlertEmailEnabled, _inpMinTradingHour, _inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        if (_inpSweetSpot1Upper <= 0 || _inpSweetSpot2Upper <= 0 || _inpSweetSpot1Lower <= 0 || _inpSweetSpot2Lower <= 0) {
            Print("Invalid sweet spot pip values must be > 0");
            return(INIT_FAILED);
        }

        if (_inpSweetSpot1Upper < _inpSweetSpot1Lower) {
            Print("Invalid sweet spot 1 pip value - upper must exceed lower");
            return(INIT_FAILED);
        }

        if (_inpSweetSpot2Upper < _inpSweetSpot2Lower) {
            Print("Invalid sweet spot 2 pip value - upper must exceed lower");
            return(INIT_FAILED);
        }

        Print("Custom initialisation for trend EA");

        ArraySetAsSeries(_longTermTimeFrameData, true);
        ArraySetAsSeries(_veryLongTermTimeFrameData, true);
        ArraySetAsSeries(_longTermTrendData, true);
        ArraySetAsSeries(_mediumTermTrendData, true);
        ArraySetAsSeries(_shortTermTrendData, true);
                
        _longTermTimeFrameHandle = iMA(_Symbol, PERIOD_H4, _inpH4MAPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_longTermTimeFrameHandle == INVALID_HANDLE) {
            Print("Error creating long term timeframe indicator");
            return(INIT_FAILED);
        }

        _veryLongTermTimeFrameHandle = iMA(_Symbol, PERIOD_H4, _inpLongTermPeriod, 0, MODE_SMA, PRICE_CLOSE);
        if (_veryLongTermTimeFrameHandle == INVALID_HANDLE) {
            Print("Error creating very long term MA indicator");
            return(INIT_FAILED);
        }

        _longTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, _inpLongTermPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_longTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating long term MA indicator");
            return(INIT_FAILED);
        }

        _mediumTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, _inpMediumTermPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_mediumTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating medium term MA indicator");
            return(INIT_FAILED);
        }

        _shortTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, _inpShortTermPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_shortTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating short term MA indicator");
            return(INIT_FAILED);
        }
                
        _accountMarginMode = AccountInfoInteger(ACCOUNT_MARGIN_MODE);
        if (_accountMarginMode == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING) {
            Print("Headging mode set");
        }

        PrintAccountInfo();
    }

    return retCode;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    Print("Releasing indicator handles");
    
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_mediumTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);
    ReleaseIndicator(_longTermTimeFrameHandle);    
    ReleaseIndicator(_veryLongTermTimeFrameHandle);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{   
    // -------------------- Collect most current data --------------------
    if (!RefreshRates()) {
        Print("Could not refresh rates during processing.");
        return;
    }

    int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 40, _prices);
    if (numberOfPriceDataPoints <= 0) {
        Print("Error copying rates during processing.");
        return;
    }

    _isNewBar = IsNewBar(iTime(0));

    // -------------------- EXITS --------------------
    if (PositionSelect(_Symbol) == true) // We have an open position
    {
        CheckToModifyPositions();
    }

    //--- we work only at the time of the birth of new bar
    if (!_isNewBar) return;

    // -------------------- ENTRIES --------------------  
    if (PositionSelect(_Symbol) == false) // We have no open positions
    {
        _barsSincePositionClosed++;
        if (IsOutsideTradingHours()) {
            return;
        }

        numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 40, _prices);
        if (numberOfPriceDataPoints <= 0) {
            Print("Error copying rates during processing.");
            return;
        }

        double stopLossLevel;
        double lotSize;

        NewBarAndNoCurrentPositions();

        if (_inpGoLong && HasBullishSignal()) {
            stopLossLevel = CalculateStopLossLevelForBuyOrder();
            lotSize = _fixedRisk.CheckOpenLong(_currentAsk, stopLossLevel);            
            OpenPosition(_Symbol, ORDER_TYPE_BUY, lotSize, _currentAsk, stopLossLevel, 0.0);
        }
        else if (_inpGoShort && HasBearishSignal()) {
            stopLossLevel = CalculateStopLossLevelForSellOrder();
            lotSize = _fixedRisk.CheckOpenShort(_currentBid, stopLossLevel);
            OpenPosition(_Symbol, ORDER_TYPE_SELL, lotSize, _currentBid, stopLossLevel, 0.0);
        }
    }
    else {
        _barsSincePositionOpened++;
    }
}

//+------------------------------------------------------------------+
//| Expert trade event
//+------------------------------------------------------------------+
void OnTrade()
{
    _eventCount++;

    // The OnTrade event fires multiples times.  We're only interested in handling it on the third time.
    if (_eventCount < 3) return;
    _eventCount = 0;

    if (PositionSelect(_Symbol) == true) { // We have an open position
        _currentPositionType = _position.PositionType();
        return;
    }

    int minutes = 1;
    datetime to = TimeCurrent();
    datetime from = to - 60 * minutes;

    if (!HistorySelect(from, to)) {
        Print("Failed to retrieve order history");
    }

    int ordersTotal = HistoryOrdersTotal();
    if (ordersTotal <= 0) {
        Print("Orders total was 0");
        return;
    }

    // Select the last order to work with
    ulong ticket = HistoryOrderGetTicket(ordersTotal - 1);
    if (ticket == 0) {
        Print("Couldn't get last order ticket");
        return;
    }

    long orderState = HistoryOrderGetInteger(ticket, ORDER_STATE);
    if (orderState != ORDER_STATE_FILLED) {
        Print("Order state was not filled");
        return;
    }

    if (HistoryOrderGetString(ticket, ORDER_SYMBOL) != _symbol.Name()) {
        Print("Order was for a different pair");
        return;
    }

    long orderType = HistoryOrderGetInteger(ticket, ORDER_TYPE);
    bool reset = false;

    if (_currentPositionType == POSITION_TYPE_BUY && orderType == ORDER_TYPE_SELL) {
        reset = true;
    }
    else if (_currentPositionType == POSITION_TYPE_SELL && orderType == ORDER_TYPE_BUY) {
        reset = true;
    }

    if (reset) {     
        ResetState();        
    }
}

int InitFromBase(
    double inpDynamicSizingRiskPerTrade, 
    STOPLOSS_RULE inpInitialStopLossRule, 
    int inpInitialStopLossPips, 
    bool inpGoLong, 
    bool inpGoShort, 
    bool inpAlertTerminalEnabled, 
    bool inpAlertEmailEnabled,
    int inpMinTradingHour,
    int inpMaxTradingHour)
{
    if (!_symbol.Name(Symbol())) // sets symbol name
        return(INIT_FAILED);

    if (!RefreshRates()) {
        Print("Could not refresh rates - init failed.");
        return(INIT_FAILED);
    }

    if (inpInitialStopLossRule != StaticPipsValue && inpInitialStopLossRule != CurrentBarNPips && inpInitialStopLossRule != MediumTermMA && inpInitialStopLossPips != 0) {
        Print("Invalid initial stop loss rule.  Pips should be 0 when not using StaticPipsValue, CurrentBarNPips or MediumTermMA - init failed.");
        return(INIT_FAILED);
    }

    if ((inpInitialStopLossRule == StaticPipsValue || inpInitialStopLossRule == CurrentBarNPips) && inpInitialStopLossPips <= 0) {
        Print("Invalid initial stop loss pip value.  Pips should be greater than 0 - init failed.");
        return(INIT_FAILED);
    }

    if (inpMinTradingHour < 0 || inpMinTradingHour > 23) {
        Print("Invalid min trading hour. Value should be between 0 and 23 - init failed.");
        return(INIT_FAILED);
    }

    if (inpMaxTradingHour < 0 || inpMaxTradingHour > 23) {
        Print("Invalid max trading hour. Value should be between 0 and 23 - init failed.");
        return(INIT_FAILED);
    }

    if (inpMaxTradingHour < inpMinTradingHour) {
        Print("Invalid min/max trading hours. Min should be less than or equal to max - init failed.");
        return(INIT_FAILED);
    }

    if (inpInitialStopLossRule == None) {
        Print("Invalid stop loss rules - both initial and trailing are set to None - init failed.");
        return(INIT_FAILED);
    }

    ArraySetAsSeries(_prices, true);

    _digits_adjust = 1;
    if (_Digits == 5 || _Digits == 3 || _Digits == 1) {
        _digits_adjust = 10;
    }

    _adjustedPoints = _symbol.Point() * _digits_adjust;

    if (!_fixedRisk.Init(&_symbol, PERIOD_CURRENT, 1)) {
        Print("Couldn't initialise fixed risk instance");
        return(INIT_FAILED);
    }    

    _alreadyMovedToBreakEven = false;

    printf("DA=%f, adjusted points = %f", _digits_adjust, _adjustedPoints);

    ResetState();

    return(INIT_SUCCEEDED);
}

void ResetState()
{
    _recentHigh = 0;
    _fixedRisk.Percent(_inpDynamicSizingRiskPerTrade);
    _alreadyMovedToBreakEven = false;
    _initialStop = 0;
    _barsSincePositionClosed = 0;
}

void ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}

// Warning: This function should only be called once
bool IsNewBar(datetime currentTime)
{
    bool result = false;
    if (_barTime != currentTime)
    {
        _barTime = currentTime;
        result = true;
    }

    return result;
}

////+------------------------------------------------------------------+ 
////| Get Time for specified bar index                                 | 
////+------------------------------------------------------------------+ 
datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT)
{
    if (symbol == NULL)
        symbol = _symbol.Name();
    if (timeframe == 0)
        timeframe = Period();
    datetime Time[1];
    datetime time = 0;
    int copied = CopyTime(symbol, timeframe, index, 1, Time);
    if (copied > 0) 
        time = Time[0];

    return time;
}

//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates()
{
    //--- refresh rates
    if (!_symbol.RefreshRates())
        return false;

    //--- protection against the return value of "zero"
    if (_symbol.Ask() == 0 || _symbol.Bid() == 0)
        return false;
    //---

    _currentBid = _symbol.Bid();
    _currentAsk = _symbol.Ask();

    return true;
}

void NewBarAndNoCurrentPositions()
{
    int count = CopyBuffer(_longTermTrendHandle, 0, 0, _inpLongTermPeriod, _longTermTrendData);
    if (count <= 0) {
        Print("Error copying long term trend data.");
        return;
    }
    count = CopyBuffer(_mediumTermTrendHandle, 0, 0, _inpMediumTermPeriod, _mediumTermTrendData);
    if (count <= 0) {
        Print("Error copying medium term trend data.");
        return;
    }

    count = CopyBuffer(_shortTermTrendHandle, 0, 0, _inpShortTermPeriod, _shortTermTrendData);
    if (count <= 0) {
        Print("Error copying short term trend data.");
        return;
    }

    count = CopyBuffer(_longTermTimeFrameHandle, 0, 0, _inpH4MAPeriod, _longTermTimeFrameData);
    if (count <= 0) {
        Print("Error copying long term timeframe data.");
        return;
    }

    count = CopyBuffer(_veryLongTermTimeFrameHandle, 0, 0, _inpH4MAPeriod, _veryLongTermTimeFrameData);
    if (count <= 0) {
        Print("Error copying very long term timeframe data.");
        return;
    }
}

void CheckToModifyPositions()
{
    if (!_position.Select(Symbol())) {
        return;
    }

    if (_position.PositionType() == POSITION_TYPE_BUY) {
        CheckToModifyLong();
    }
    else {
        CheckToModifyShort();
    }
}

void CheckToModifyLong()
{
    if (ShouldMoveLongToBreakEven(0.0)) {
        CloseHalf(true);
        _alreadyMovedToBreakEven = true;
        ModifyLongPosition(_position.PriceOpen(), _position.TakeProfit());
        return;
    }

    if (!_isNewBar) {
        return;
    }

    if (_barsSincePositionOpened < 3) {
        return;
    }

    int count = CopyBuffer(_mediumTermTrendHandle, 0, 0, _inpMediumTermPeriod, _mediumTermTrendData);
    if (count <= 0) {
        Print("Error copying medium term trend data.");
        return;
    }

    if (_prices[1].close < _mediumTermTrendData[1]) {
        ClosePosition();
    }
}

bool ShouldMoveLongToBreakEven(double newStop)
{
    if (_alreadyMovedToBreakEven) return false;

    double breakEvenPrice = _position.PriceOpen() * 2 - _initialStop;
    if (_currentAsk > breakEvenPrice && (newStop == 0.0 || breakEvenPrice > newStop)) {
        printf("Moving to breakeven now that the price has reached %f", breakEvenPrice);

        /* This has changed quite a bit recently.  Historically, we would always move the stop to breakeven.
        Then this was removed so we don't move the stop
        AND NOW...we move only if Martingale is active, meaning we have increased our risk beyond normal.
        This is a way to recover our losses quickly and manage the risk a little better.
        */
        //if (_martingaleActive) {
        //    //newStop = _position.PriceOpen();

        //    newStop = breakEvenPrice;
        //}
        return true;
    }

    return false;
}


//void CheckToModifyLong()
//{
//    //if (ShouldMoveLongToBreakEven(0.0)) {
//    //    CloseHalf(true);
//    //    _alreadyMovedToBreakEven = true;
//    //    ModifyLongPosition(_position.PriceOpen(), _position.TakeProfit());
//    //    return;
//    //}
//
//    //if (!(_isNewBar && _barsSincePositionOpened >= 3)) {
//    //    return;
//    //}
//    
//    // New change - only trail the SL ON THE SECOND TRANCHE (otherwise we just use the inital SL), and only if we touch the short term MA
//    
//    // CHANGE - We don't have to be already at breakeven - this will cut our losses more quickly    
//    //if (!_alreadyMovedToBreakEven) return;
//
//    double newStop = 0;
//
//    if (!_alreadyMovedToBreakEven) {
//        // Are we making higher highs?
//        if (_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh) {
//            _recentHigh = _prices[1].high;
//        }
//        else {
//            // Check if our profit has fallen by 50%
//            //if (DecentProfitSoFar()) {
//            if (_isNewBar && ProfitDroppedByHalf()) {
//                CloseHalf(true);
//                newStop = _position.PriceOpen();
//            }
//            //}
//        }
//    }
//
//    if (_isNewBar) {
//        int count = CopyBuffer(_mediumTermTrendHandle, 0, 0, _inpMediumTermPeriod, _mediumTermTrendData);
//        if (count <= 0) {
//            Print("Error copying medium term trend data.");
//            return;
//        }
//
//        if (_currentAsk < _mediumTermTrendData[1]) {
//            // Set SL to current low + margin
//            newStop = _prices[1].low - _adjustedPoints * _inpTrailingStopLossPips;
//        }
//    }
//
//    if (newStop == 0) {
//        return;
//    }
//
//    if (ModifyLongPosition(newStop, _position.TakeProfit())) {
//    }
//}
//
//bool DecentProfitSoFar()
//{
//    double largestProfit = _recentHigh - _position.PriceOpen();
//    double initialRisk = _position.PriceOpen() - _initialStop;
//
//    if (largestProfit > initialRisk * 0.75) {
//        Print("Trade has made a decent profit so far");
//        return true;
//    }
//
//    return false;
//}
//
//bool ProfitDroppedByHalf()
//{
//    double diff = _recentHigh - _currentAsk;
//    double largestProfit = _recentHigh - _position.PriceOpen();
//
//    if (diff > largestProfit / 2) {
//        Print("Price dropped to below half profit");
//        return true;
//    }
//
//    return false;
//}

void CheckToModifyShort()
{
    if (ShouldMoveShortToBreakEven(0.0)) {
        CloseHalf(false);
        _alreadyMovedToBreakEven = true;
        ModifyShortPosition(_position.PriceOpen(), _position.TakeProfit());
        return;
    }

    /*if (!(_isNewBar && _barsSincePositionOpened >= 3)) {
        return;
    }*/

    // New change - only trail the SL ON THE SECOND TRANCHE (otherwise we just use the inital SL), and only if we touch the short term MA
    
    //if (!_alreadyMovedToBreakEven) return;

    int count = CopyBuffer(_shortTermTrendHandle, 0, 0, _inpShortTermPeriod, _shortTermTrendData);
    if (count <= 0) {
        Print("Error copying short term trend data.");
        return;
    }

    if (_isNewBar) {
        _trailedThisBar = false;
    }

    if (!_trailedThisBar && _currentBid > _shortTermTrendData[1]) {
        if (ModifyShortPosition(_currentBid + _inpTrailingStopLossPips * _adjustedPoints, _position.TakeProfit())) {
        //if (ModifyShortPosition(_currentBid + 5 * _adjustedPoints, _position.TakeProfit())) {
            _trailedThisBar = true;
        }
    }
}

bool ShouldMoveShortToBreakEven(double newStop)
{
    if (_alreadyMovedToBreakEven) return false;

    double breakEvenPrice = _position.PriceOpen() * 2 - _initialStop;
    if (_currentBid < breakEvenPrice && (newStop == 0.0 || breakEvenPrice < newStop)) {
        printf("Moving to breakeven now that the price has reached %f", breakEvenPrice);

        /* ACTUALLY MOVE IT - Needs more thorough testing */
        // Changing this so we don't actually move the SL

        /* This has changed quite a bit recently.  Historically, we would always move the stop to breakeven.
        Then this was removed so we don't move the stop
        AND NOW...we move only if Martingale is active, meaning we have increased our risk beyond normal.
        This is a way to recover our losses quickly and manage the risk a little better.
        */
        //if (_martingaleActive) {
        //    //newStop = _position.PriceOpen();

        //    newStop = breakEvenPrice;
        //}

        return true;
    }

    return false;
}

void CloseHalf(bool isLong)
{
    double halfVolume = _position.Volume() / 2;
    double stepvol = _symbol.LotsStep(); // Usually 0.01
    halfVolume = MathFloor(halfVolume / stepvol) * stepvol;
    bool success;
    string comment;

    if (isLong) {
        comment = "Closed half long at profit";
        if (_accountMarginMode == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING) {
            success = _trade.PositionClosePartial(_Symbol, halfVolume);
        }
        else {
            success = _trade.Sell(halfVolume, _Symbol, 0.0, 0.0, 0.0, comment);
        }        
    }
    else {
        comment = "Closed half short at profit";
        if (_accountMarginMode == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING) {
            success = _trade.PositionClosePartial(_Symbol, halfVolume);
        }
        else {
            success = _trade.Buy(halfVolume, _Symbol, 0.0, 0.0, 0.0, comment);
        }        
    }

    if (!success) {
        printf("Failed to close half position for volume of %f", halfVolume);
        Print("Return code=", _trade.ResultRetcode(),
            ". Code description: ", _trade.ResultRetcodeDescription());
    }
}

void ClosePosition()
{
    Print("Closing position");
    bool success = _trade.PositionClose(_Symbol);
    if (!success) {
        Print("Failed to close position");
        Print("Return code=", _trade.ResultRetcode(), ". Code description: ", _trade.ResultRetcodeDescription());
    }
}

bool ModifyLongPosition(double newStop, double takeProfit)
{
    double sl = NormalizeDouble(newStop, _symbol.Digits());
    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

    if (_position.StopLoss() < sl || _position.StopLoss() == 0.0) {
        double diff = (_currentAsk - sl) / _adjustedPoints;
        if (diff < stopLevelPips) {
            printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
                _currentAsk, sl, stopLevelPips, diff);

            sl = _currentAsk - stopLevelPips * _adjustedPoints;
        }

        //--- modify position
        if (!_trade.PositionModify(Symbol(), sl, takeProfit)) {
            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f", sl, takeProfit);
        }

        if (!_alreadyMovedToBreakEven && sl >= _position.PriceOpen()) {
            _alreadyMovedToBreakEven = true;
        }

        return true;
    }

    return false;
}

bool ModifyShortPosition(double newStop, double takeProfit)
{
    double sl = NormalizeDouble(newStop, _symbol.Digits());
    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

    if (_position.StopLoss() > sl || _position.StopLoss() == 0.0) {
        double diff = (sl - _currentBid) / _adjustedPoints;
        if (diff < stopLevelPips) {
            printf("Can't set new stop that close to the current price.  Bid = %f, new stop = %f, stop level = %f, diff = %f",
                _currentBid, sl, stopLevelPips, diff);

            sl = _currentBid + stopLevelPips * _adjustedPoints;
        }

        //--- modify position
        if (!_trade.PositionModify(Symbol(), sl, takeProfit)) {
            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f", sl, takeProfit);
        }

        if (!_alreadyMovedToBreakEven && sl <= _position.PriceOpen()) {
            _alreadyMovedToBreakEven = true;
        }

        return true;
    }

    return false;
}

void OpenPosition(string symbol, ENUM_ORDER_TYPE orderType, double volume, double price, double stopLoss, double takeProfit)
{
    string message;
    string orderTypeMsg;

    switch (orderType) {
        case ORDER_TYPE_BUY:
            orderTypeMsg = "Buy";
            message = "Going long. Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_SELL:
            orderTypeMsg = "Sell";
            message = "Going short. Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_BUY_LIMIT:
            orderTypeMsg = "Buy limit";
            message = "Going long at " + (string)price + ". Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_SELL_LIMIT:
            orderTypeMsg = "Sell limit";
            message = "Going short at " + (string)price + ". Magic Number #" + (string)_trade.RequestMagic();
            break;
    }

    if (_inpAlertTerminalEnabled) {
        Alert(message);
    }

    if (_trade.PositionOpen(symbol, orderType, volume, price, stopLoss, takeProfit, message)) {
        _initialStop = 0;
        uint resultCode = _trade.ResultRetcode();
        if (resultCode == TRADE_RETCODE_PLACED || resultCode == TRADE_RETCODE_DONE) {
            Print("Entry rules: A ", orderTypeMsg, " order has been successfully placed with Ticket#: ", _trade.ResultOrder());
            _barsSincePositionOpened = 0;
            _initialStop = stopLoss;
        }
        else {
            Print("Entry rules: The ", orderTypeMsg, " order request could not be completed.  Result code: ", resultCode, ", Error: ", GetLastError());
            ResetLastError();
            return;
        }
    }
}

bool IsOutsideTradingHours()
{
    MqlDateTime currentTime;
    TimeToStruct(TimeCurrent(), currentTime);
    if (_inpMinTradingHour > 0 && currentTime.hour < _inpMinTradingHour) {
        return true;
    }

    if (_inpMaxTradingHour > 0 && currentTime.hour > _inpMaxTradingHour) {
        return true;
    }

    return false;
}

double CalculateStopLossLevelForBuyOrder()
{
    double stopLossPipsFinal;
    double stopLossLevel = 0;
    double stopLevelPips;
    double low;
    double priceFromStop;
    double pips = 5;

    stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel
    switch (_inpInitialStopLossRule) {
        case CurrentBar2Pips:
            // Fall-through
        case CurrentBarNPips:
            // Fall-through
        case CurrentBar5Pips:
            if (_inpInitialStopLossRule == CurrentBar2Pips) {
                pips = 2;
            }
            else if (_inpInitialStopLossRule == CurrentBarNPips) {
                pips = _inpInitialStopLossPips;
            }

            stopLossLevel = _prices[1].low - _adjustedPoints * pips;
            priceFromStop = (_currentAsk - stopLossLevel) / (_Point * _digits_adjust);

            if (priceFromStop < stopLevelPips) {
                printf("calculated stop too close to price.  adjusting from %f to %f", priceFromStop, stopLevelPips);
                stopLossPipsFinal = stopLevelPips;
            }
            else {
                stopLossPipsFinal = priceFromStop;
            }

            stopLossLevel = _currentAsk - stopLossPipsFinal * _Point * _digits_adjust;
            break;

        case PreviousBar5Pips:
            low = _prices[1].low;
            if (_prices[2].low < low) {
                low = _prices[2].low;
            }

            stopLossLevel = low - _adjustedPoints * 5;
            break;

        case PreviousBar2Pips:
            low = _prices[1].low;
            if (_prices[2].low < low) {
                low = _prices[2].low;
            }

            stopLossLevel = low - _adjustedPoints * 2;
            break;

        case StaticPipsValue:
            if (_inpInitialStopLossPips < stopLevelPips) {
                stopLossPipsFinal = stopLevelPips;
            }
            else {
                stopLossPipsFinal = _inpInitialStopLossPips;
            }

            stopLossLevel = _currentAsk - stopLossPipsFinal * _Point * _digits_adjust;
            break;

        case MediumTermMA:
            stopLossLevel = MathMin(_mediumTermTrendData[1], _prices[1].low - _adjustedPoints * _inpInitialStopLossPips);
            break;

        case LongTermMA:
            stopLossLevel = MathMin(_longTermTrendData[1] - _adjustedPoints * 5, _prices[1].low - _adjustedPoints * _inpInitialStopLossPips);
            break;

        case None:
            stopLossLevel = 0;
            break;
    }

    double sl = NormalizeDouble(stopLossLevel, _symbol.Digits());
    return sl;
}

double CalculateStopLossLevelForSellOrder()
{
    double stopLossPipsFinal;
    double stopLossLevel = 0;
    double stopLevelPips;
    double high;
    double pips = 5;

    stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel
    switch (_inpInitialStopLossRule) {
    case None:
        stopLossLevel = 0;
        break;

    case StaticPipsValue:
        if (_inpInitialStopLossPips < stopLevelPips) {
            stopLossPipsFinal = stopLevelPips;
        }
        else {
            stopLossPipsFinal = _inpInitialStopLossPips;
        }

        stopLossLevel = _currentBid + stopLossPipsFinal * _Point * _digits_adjust;
        break;

    case CurrentBar2Pips:
        // Fall-through
    case CurrentBarNPips:
        // Fall-through
    case CurrentBar5Pips:
        if (_inpInitialStopLossRule == CurrentBar2Pips) {
            pips = 2;
        }
        else if (_inpInitialStopLossRule == CurrentBarNPips) {
            pips = _inpInitialStopLossPips;
        }

        stopLossLevel = _prices[1].high + _adjustedPoints * pips;
        break;

    case PreviousBar5Pips:
        high = _prices[1].high;
        if (_prices[2].high > high) {
            high = _prices[2].high;
        }

        stopLossLevel = high + _adjustedPoints * 5;
        break;

    case PreviousBar2Pips:
        high = _prices[1].high;
        if (_prices[2].high > high) {
            high = _prices[2].high;
        }

        stopLossLevel = high + _adjustedPoints * 2;
        break;

    case MediumTermMA:
        stopLossLevel = MathMax(_mediumTermTrendData[1], _prices[1].high + _adjustedPoints * _inpInitialStopLossPips);
        break;

    case LongTermMA:
        stopLossLevel = MathMax(_longTermTrendData[1] + _adjustedPoints * 5, _prices[1].high + _adjustedPoints * _inpInitialStopLossPips);
        break;
    }

    double priceFromStop = (stopLossLevel - _currentBid) / (_Point * _digits_adjust);

    Print("Price from stop: ", priceFromStop);
    if (priceFromStop < stopLevelPips) {
        printf("calculated stop too close to price.  adjusting from %f to %f", priceFromStop, stopLevelPips);
        stopLossPipsFinal = stopLevelPips;
    }
    else {
        stopLossPipsFinal = priceFromStop;
    }

    stopLossLevel = _currentBid + stopLossPipsFinal * _Point * _digits_adjust;

    return NormalizeDouble(stopLossLevel, _symbol.Digits());
}

bool HasBullishSignal()
{
    /*
    OLD RULES...

    Rule 1 - We must have closed near the high (i.e. had an up bar)
    Rule 2 - The slope of the long-term trend must be up
    Rule 3 - The current low must be higher than long-term MA (or maybe within 6 pips?)
    Rule 4 - The current low must be less than or equal to the short-term MA or within a pip or two (or perhaps at least one recent bar's low was lower)
    Rule 5 - The current close is higher than the short-term MA
    Rule 6 - The slope of the short-term MA must be flat or rising
    Rule 7 - There hasn't been a recent (in the last 15 or 20 bars) highest high that is more than a certain level above the current close (using ATR)
    */

    /*
    NEW RULES FOR GOING LONG
    -------------------------

    Rule 1 - on the 4H timeframe, the price must be higher than the short term MA (default 50 period EMA)
    Rule 2 - on the 4H timeframe, the price must be higher than the long term MA (default 240 period LWMA)
    Rule 3 - The H4 short term MA must be above the H4 long term MA
    Rule 4 - on the 15M timeframe, The price must be higher than the long term MA
    Rule 5 - on the 15M timeframe, The MACD must be below 0.001
    Rule 6 - on the 15M timeframe, the price must go into the MA and then we get a buy (QMP Filter) signal
    Rule 7 - The price must not have recently closed lower than the medium term MA
    Rule 8 - The current low must be higher than the medium term MA
    Rule 9 - The latest candle must be bullish (closed higher than open)
    Rule 10 - There must be a sufficient gap between the short and long term MAs
    Rule 11 - Out of the last x (say 10) number of bars, there should be no more than y (say 3) bars that have a high < short term MA
    Rule 12 - The difference between the short term MA and medium term MA must not be more than 20 pips

    -- The stop loss should be set to a few pips lower than the bar that recently touched the MA
    NEW SL rule - Set the SL to the medium term average!!
    */

    /*
    if (_prices[1].close <= _longTermTimeFrameData[1]) return false;
    if (_prices[1].close <= _veryLongTermTimeFrameData[1]) return false;
    if (_longTermTimeFrameData[1] <= _veryLongTermTimeFrameData[1]) return false;
    if (_prices[1].close <= _longTermTrendData[1]) return false;
    if (_macdData[1] >= 0.001) return false;
    if (GetTrendDirection(1) != "Up") return false;
    if (!PriceWentLowerThanMA()) return false;
    if (PriceClosedLowerThanMediumTermMA()) return false;
    if (_prices[1].close <= _prices[1].open) return false;
    if (_shortTermTrendData[1] - _longTermTrendData[1] <= _adjustedPoints * 12) return false;
    if (PriceRecentlyLowerThanShortTermMA()) return false;
    if (MediumMinusShortMA() >= 20) return false;
    */

    /*  New ideas:
        Measure distance between short and medium term MA
        Measure range of latest bar - do it open near low and close near high?
        Have we made a new high over the last x bars?
        Measure slope of MAs (use 35 bars on 15M)
        Ignore double risk reward or increase to 4x
        Don't trail SL when we get a bad signal
        Sell half when we move to breakeven
        Simply sell when we close below short term MA
        Use a Buy Stop Order a few pips above signal bar as confirmation of trend
    */

    // has not had a higher high in the last 15 bars by more than 10 pips


    /* IDEA: Check that on long term timeframe, price recently moved into the zone, i.e. back towards the short MA
    e.g. for shorts, highest high in the last 10 bars > short term MA on H4


    Measure distance between short and long on H4
    Measure distance between highest high and lowest low over x bars on H4
    Sell half on first QMP filter signal
    Test getting out on break of medium term MA regardless of whether we hit breakeven or not
    
    
    Use QMP filter signal for 2nd tranch   
    */

    // Rule 1 - on the H4 timeframe, the price must be higher than the long term MA (default 89 period EMA)
    if (_prices[1].close <= _veryLongTermTimeFrameData[1]) return false;

    // Rule 2 - on the H4 timeframe, the price must be higher than the short term MA (default 21 period EMA)
    if (_prices[1].close <= _longTermTimeFrameData[1]) return false;

    // Rule 3 - The H4 short term MA must be above the H4 long term MA
    if (_longTermTimeFrameData[1] <= _veryLongTermTimeFrameData[1]) return false;

    // Rule 4 - The price must be above the H4 short term MA
    if (_prices[1].close <= _longTermTimeFrameData[1]) return false;

    // Rule 5 - The price must be above all 3 MAs and all 3 MAs "layered"
    if (!MAsLayered()) return false;

    // Rule 6 - The latest candle must be bullish (closed higher than open)
    if (!IsLatestCandleBullish()) return false;
    
    // Rule 7 - We have made a higher high in the last 15 bars
    int index = HighestHighIndex();
    if (index != 1) return false;
    
    // Rule 8 - The short term MA must be above the medium term MA by between 4 and 8 pips
    if (!InShortTermMASweetSpot()) return false;

    // Rule 9 - The medium term MA must be above the long term MA by between 2 and 6 pips
    if (!InMediumTermMASweetSpot()) return false;

    // Rule 11 - Price has closed within a few pips of both the short term and long term MAs over the last x bars
    //if (!PriceHasTradedCloseToShortTermMA()) return false;
    //if (!PriceHasTradedCloseToLongTermMA()) return false;

    return true;
}

bool MAsLayered()
{
    if (_shortTermTrendData[1] < _longTermTrendData[1]) return false;
    if (_mediumTermTrendData[1] < _longTermTrendData[1]) return false;
    if (_shortTermTrendData[1] < _mediumTermTrendData[1]) return false;

    if (_prices[1].close < _shortTermTrendData[1]) return false;
    if (_prices[1].close < _longTermTrendData[1]) return false;

    return true;
}

bool PriceHasTradedCloseToShortTermMA()
{
    for (int i = 2; i < _inpShortTermPeriod; i++) {
        double diff = MathAbs(_shortTermTrendData[i] - _prices[i].close) / _adjustedPoints;
        if (diff > 14) {
            printf("Price closed outside short term MA (%d, %f, %f)", i, _prices[i].close, _shortTermTrendData[i]);
            return false;
        }
    }

    return true;
}

bool PriceHasTradedCloseToLongTermMA()
{
    for (int i = 2; i < _inpShortTermPeriod; i++) {
        double diff = MathAbs(_longTermTrendData[i] - _prices[i].close) / _adjustedPoints;
        if (diff > 14) {
            printf("Price closed outside long term MA (%d, %f, %f)", i, _prices[i].close, _longTermTrendData[i]);
            return false;
        }
    }

    return true;
}

bool IsLatestCandleBullish()
{
    double diff = _prices[1].high - _prices[1].close;
    return diff < _prices[1].close - _prices[1].low;
}

int HighestHighIndex()
{
    int index = iHighest(NULL, 0, MODE_CLOSE, _inpHighestHighNumBars, 1);
    if (index != -1) {
        return index;
    }
    else {
        PrintFormat("iHighest() call error. Error code=%d", GetLastError());
    }

    return -1;
}

bool InShortTermMASweetSpot()
{
    double shortTermDiff = (_shortTermTrendData[1] - _mediumTermTrendData[1]) / _adjustedPoints;
    if (shortTermDiff >= _inpSweetSpot1Lower && shortTermDiff <= _inpSweetSpot1Upper) {
        return true;
    }

    return false;
}

bool InMediumTermMASweetSpot()
{
    double mediumTermDiff = (_mediumTermTrendData[1] - _longTermTrendData[1]) / _adjustedPoints;
    if (mediumTermDiff >= _inpSweetSpot2Lower && mediumTermDiff <= _inpSweetSpot2Upper) {
        return true; 
    }

    return false;
}

double MediumMinusShortMA()
{
    double diff = MathAbs(_mediumTermTrendData[1] - _shortTermTrendData[1]) / _adjustedPoints;
    return diff;
}

// Helper methods to detect bullish setup
bool PriceWentLowerThanMA()
{
    for (int i = 2; i <= 4; i++) {
        if (_prices[i].low < _shortTermTrendData[i]) {
            return true;
        }
    }

    return false;
}

bool PriceClosedLowerThanMediumTermMA()
{
    for (int i = 2; i <= 4; i++) {
        if (_prices[i].close < _mediumTermTrendData[i]) {
            return true;
        }
    }

    return false;
}

bool PriceRecentlyLowerThanShortTermMA()
{
    int count = 0;
    for (int i = 2; i <= 10; i++) {
        if (_prices[i].high < _shortTermTrendData[i]) {
            count++;
        }
    }

    return count > 3;
}


bool HasBearishSignal()
{
    //_longTermTimeFrameData[1]



    /*
    OLD RULES

    Rule 1 - We must have closed near the low (i.e. had a down bar)
    Rule 2 - The slope of the long-term trend must be down
    Rule 3 - The current high must be lower than the long-term MA (or maybe within 6 pips?)
    Rule 4 - The current high must be greater than or equal to the short-term MA or within a pip or two (or perhaps at least one recent bar's low was lower)
    Rule 5 - The current close must be lower than the short-term MA
    Rule 6 - The slope of the short-term MA is flat or down
    Rule 7 - There hasn't been a recent (in the last 15 bars) lowest low that is more than a certain level below the current close (using ATR)
    */

    /* 
    NEW RULES FOR GOING SHORT
    -------------------------

    Rule 1 - on the 4H timeframe, the price must be lower than the short term MA (default 50 period EMA)
    Rule 2 - on the 4H timeframe, the price must be lower than the long term MA (default 240 period LWMA)
    Rule 3 - The H4 short term MA must be below the H4 long term MA
    Rule 4 - on the 15M timeframe, The price must be lower than the long term MA
    Rule 5 - on the 15M timeframe, The MACD must be above 0.001
    Rule 6 - on the 15M timeframe, the price must go into the MA and then we get a sell (QMP Filter) signal
    Rule 7 - The price must not have recently closed higher than the medium term MA
    Rule 8 - The current high must be lower than the medium term MA
    Rule 9 - The latest candle must be bearish (closed lower than open)
    Rule 10 - There must be a sufficient gap between the short and long term MAs
    Rule 11 - Out of the last x (say 10) number of bars, there should be no more than y (say 3) bars that have a low > short term MA

    -- The stop loss should be set to a few pips higher than the bar that recently touched the MA
    NEW SL rule - Set the SL to the medium term average!!
    */

    /*
    if (_prices[1].close >= _longTermTimeFrameData[1]) return false;
    if (_prices[1].close >= _veryLongTermTimeFrameData[1]) return false;
    if (_longTermTimeFrameData[1] >= _veryLongTermTimeFrameData[1]) return false;
    if (_prices[1].close >= _longTermTrendData[1]) return false;
    if (_macdData[1] <= 0.001) return false;
    if (GetTrendDirection(1) != "Dn") return false;
    if (!PriceWentHigherThanMA()) return false;
    if (PriceClosedHigherThanMediumTermMA()) return false;
    if (_prices[1].close >= _prices[1].open) return false;
    if (_longTermTrendData[1] - _shortTermTrendData[1] <= _adjustedPoints * 12) return false;
    if (PriceRecentlyHigherThanShortTermMA()) return false;
    if (MediumMinusShortMA() >= 20) return false;

    // TEST extra RULE: If recent (say 4 bars) price HIGH > medium term MA then do not enter
    // Test new rule - set TP at 3xrisk
    */


    /* WAVECATCHER RULES FOR GOING SHORT
    ------------------------------------

    Rule 1 - on the 4H timeframe, the price must be lower than the short term MA (default 50 period EMA)
    Rule 2 - on the 4H timeframe, the price must be lower than the long term MA (default 240 period LWMA)
    Rule 3 - The H4 short term MA must be below the H4 long term MA
    Rule 4 - The latest candle must be bearish (closed lower than open)
    Rule 5 - We have made a lower low in the last 15 bars
    Rule 6 - The short term MA must be below the medium term MA by between 4 and 8 pips
    Rule 7 - The medium term MA must be below the long term MA by between 2 and 6 pips
    */

    //if (_prices[1].close >= _longTermTimeFrameData[1]) return false;

    if (_prices[1].close >= _veryLongTermTimeFrameData[1]) return false;
    if (_longTermTimeFrameData[1] >= _veryLongTermTimeFrameData[1]) return false;
    if (!IsLatestCandleBearish()) return false;
    int index = LowestLowIndex();
    if (index != 1) return false;
    if (!InBearishShortTermMASweetSpot()) return false;
    if (!InBearishMediumTermMASweetSpot()) return false;

    return true;
}

// Helper methods to detect bearish setup
bool IsLatestCandleBearish()
{
    double diff = _prices[1].high - _prices[1].close;
    return diff > _prices[1].close - _prices[1].low;
}

int LowestLowIndex()
{
    int index = iLowest(NULL, 0, MODE_CLOSE, _inpHighestHighNumBars, 1);
    if (index != -1) {
        return index;
    }
    else {
        PrintFormat("iLowest() call error. Error code=%d", GetLastError());
    }

    return -1;
}

bool InBearishShortTermMASweetSpot()
{
    double shortTermDiff = (_mediumTermTrendData[1] - _shortTermTrendData[1]) / _adjustedPoints;
    if (shortTermDiff >= _inpSweetSpot1Lower && shortTermDiff <= _inpSweetSpot1Upper) return true;
    return false;
}

bool InBearishMediumTermMASweetSpot()
{
    double mediumTermDiff = (_longTermTrendData[1] - _mediumTermTrendData[1]) / _adjustedPoints;
    if (mediumTermDiff >= _inpSweetSpot2Lower && mediumTermDiff <= _inpSweetSpot2Upper) return true;
    return false;
}

bool PriceWentHigherThanMA()
{
    for (int i = 2; i <= 4; i++) {
        if (_prices[i].high > _shortTermTrendData[i]) {
            return true;
        }
    }

    return false;
}

bool PriceClosedHigherThanMediumTermMA()
{
    for (int i = 2; i <= 4; i++) {
        if (_prices[i].close > _mediumTermTrendData[i]) {
            return true;
        }
    }

    return false;
}

bool PriceRecentlyHigherThanShortTermMA()
{
    int count = 0;
    for (int i = 2; i <= 10; i++) {
        if (_prices[i].low > _shortTermTrendData[i]) {
            count++;
        }
    }

    return count > 3;
}

void PrintAccountInfo()
{
    //--- Name of the company 
    string company = AccountInfoString(ACCOUNT_COMPANY);
    //--- Name of the client 
    string name = AccountInfoString(ACCOUNT_NAME);
    //--- Account number 
    long login = AccountInfoInteger(ACCOUNT_LOGIN);
    //--- Name of the server 
    string server = AccountInfoString(ACCOUNT_SERVER);
    //--- Account currency 
    string currency = AccountInfoString(ACCOUNT_CURRENCY);
    //--- Demo, contest or real account 
    ENUM_ACCOUNT_TRADE_MODE account_type = (ENUM_ACCOUNT_TRADE_MODE)AccountInfoInteger(ACCOUNT_TRADE_MODE);
    //--- Now transform the value of  the enumeration into an understandable form 
    string trade_mode;
    switch (account_type)
    {
    case  ACCOUNT_TRADE_MODE_DEMO:
        trade_mode = "demo";
        break;
    case  ACCOUNT_TRADE_MODE_CONTEST:
        trade_mode = "contest";
        break;
    default:
        trade_mode = "real";
        break;
    }
    //--- Stop Out is set in percentage or money 
    ENUM_ACCOUNT_STOPOUT_MODE stop_out_mode = (ENUM_ACCOUNT_STOPOUT_MODE)AccountInfoInteger(ACCOUNT_MARGIN_SO_MODE);
    //--- Get the value of the levels when Margin Call and Stop Out occur 
    double margin_call = AccountInfoDouble(ACCOUNT_MARGIN_SO_CALL);
    double stop_out = AccountInfoDouble(ACCOUNT_MARGIN_SO_SO);

    //--- Show brief account information 
    PrintFormat("The account of the client '%s' #%d %s opened in '%s' on the server '%s'",
        name, login, trade_mode, company, server);
    PrintFormat("Account currency - %s, MarginCall and StopOut levels are set in %s",
        currency, (stop_out_mode == ACCOUNT_STOPOUT_MODE_PERCENT) ? "percentage" : " money");
    PrintFormat("MarginCall=%G, StopOut=%G", margin_call, stop_out);
    PrintFormat("Margin mode=%d", _accountMarginMode);
}