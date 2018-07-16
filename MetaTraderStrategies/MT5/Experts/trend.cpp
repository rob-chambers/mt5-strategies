//+------------------------------------------------------------------+
//|                                                          trend.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.20"
//+------------------------------------------------------------------+
//|VERSION HISTORY
//|v1.20 - Added ability to go short, mirroring rules for longs
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
    CurrentBarNPips
};

enum TREND_TYPE
{
    TREND_TYPE_HARD_DOWN = 0,   // strong down trend
    TREND_TYPE_DOWN = 1,        // down trend
    TREND_TYPE_SOFT_DOWN = 2,   // weak down trend
    TREND_TYPE_FLAT = 3,        // no trend
    TREND_TYPE_SOFT_UP = 4,     // weak up trend
    TREND_TYPE_UP = 5,          // up trend
    TREND_TYPE_HARD_UP = 6      // strong up trend
};

//+------------------------------------------------------------------------------------------------------------------------------+
//| Input variables                                                                                                              |
//+------------------------------------------------------------------------------------------------------------------------------+
input double        _inpDynamicSizingRiskPerTrade = 1;              // Risk per trade of account balance
input STOPLOSS_RULE _inpInitialStopLossRule = CurrentBarNPips;      // Initial Stop Loss Rule
input int           _inpInitialStopLossPips = 2;                    // Initial stop loss in pips

// Go Long / short parameters
input bool      _inpGoLong = true;                                  // Whether to enter long trades or not
input bool      _inpGoShort = true;                                 // Whether to enter short trades or not

// Alert parameters
input bool      _inpAlertTerminalEnabled = true;                    // Whether to show terminal alerts or not
input bool      _inpAlertEmailEnabled = false;                      // Whether to alert via email or not

// Trading time parameters
input int       _inpMinTradingHour = 0;                             // The minimum hour of the day to trade (e.g. 7 for 7am)
input int       _inpMaxTradingHour = 0;                             // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Martingale parameters
input bool      _inpUseMartingale = true;                           // Whether to double down after a losing trade or not
input int       _inpMartingalePeriod = 20;                          // The maximum number of bars after a losing trade to use the Martingale system

// Technical parameters
input int       _inpLongTermPeriod = 70;                            // The number of bars on the long term timeframe used to determine the trend
input int       _inpShortTermPeriod = 25;                           // The number of bars on the short term timeframe used to determine the trend
input double    _inpShortTermTrendRejectionMultiplier = 1.5;
input int       _inpDailyMAPeriod = 9;                              // The daily timeframe MA period

input double    _inpStrongTrendThreshold = 5;
input double    _inpStandardTrendThreshold = 3;
input double    _inpWeakTrendThreshold = 1;

input int       _inpTrailAfterGoodProfitPips = 11;                  // Trailing stop after achieving a good profit
input int       _inpSwingHighLowTrailStopPips = 3;                  // Trailing stop after a swing high/low
input int       _inpSwingHighLowTPPips = 0;                         // Take Profit margin above swing high/low upon reaching a swing high/low

//+------------------------------------------------------------------------------------------------------------------------------+
//| Private variables                                                                                                            |
//+------------------------------------------------------------------------------------------------------------------------------+

string _currentSignal;

int _qmpFilterHandle;
int _platinumHandle;
int _longTermTimeFrameHandle;
int _longTermTrendHandle;
int _mediumTermTrendHandle;
int _shortTermTrendHandle;
int _shortTermATRHandle;
int _longTermATRHandle;

double _qmpFilterUpData[];
double _qmpFilterDownData[];

double _macdData[];
double _longTermTimeFrameData[];
double _longTermTrendData[];
double _mediumTermTrendData[];
double _shortTermTrendData[];
double _shortTermATRData[];
double _longTermATRData[];

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
double _recentSwingHigh, _recentSwingLow;
bool _hadRecentSwingHigh, _hadRecentSwingLow;

// Private
datetime _barTime;                  // For detection of a new bar
double _recentHigh;                 // Tracking the most recent high for stop management
double _recentLow;                  // Tracking the most recent low for stop management
int _barsSincePositionOpened;       // Counter of the number of bars since a position was opened
int _barsSincePositionClosed;       // Counter of the number of bars since a position was closed
int _GetLastError;                  // Error code
ulong _lastOrderTicket;             // Ticket of the last processed order
int _eventCount;                    // Counter for OnTrade event
int _currentPositionType;           // The current type of position (long/short)
CMoneyFixedRisk _fixedRisk;         // Fixed risk money management class
bool _isNewBar;                     // A flag to indicate if this tick is the start of a new bar
int _losingTradeCount = 0;          // Counter of the number of consecutive losing trades 
bool _martingaleActive = false;     // A flag to indicate whether we have increased our lot size beyond the default for the current trade
int _fileHandle;

// Perf data
int _longTermRsiHandle;
double _longTermRsiData[];

double _macd;
string _dailySignal;
double _dailyMA0, _dailyMA1;
double _open, _high, _low, _close;
double _shortTermTrend, _longTermTrend;
int _upIndex, _downIndex;
double _longTermRsiDataCurrent, _longTermRsiDataPrior;
TREND_TYPE _longTimeframeTrend;
string _signalOnEntry;
double _highestHigh20, _highestHigh25;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    int retCode = InitFromBase(_inpDynamicSizingRiskPerTrade, _inpInitialStopLossRule, _inpInitialStopLossPips, _inpGoLong, _inpGoShort, _inpAlertTerminalEnabled, _inpAlertEmailEnabled, _inpMinTradingHour, _inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for trend EA");

        ArraySetAsSeries(_qmpFilterUpData, true);
        ArraySetAsSeries(_qmpFilterDownData, true);

        ArraySetAsSeries(_macdData, true);
        ArraySetAsSeries(_longTermTimeFrameData, true);
        ArraySetAsSeries(_longTermTrendData, true);
        ArraySetAsSeries(_mediumTermTrendData, true);
        ArraySetAsSeries(_shortTermTrendData, true);
        ArraySetAsSeries(_shortTermATRData, true);
        ArraySetAsSeries(_longTermATRData, true);
        ArraySetAsSeries(_longTermRsiData, true);
        
        _platinumHandle = iCustom(_Symbol, PERIOD_CURRENT, "MACD_Platinum", 12, 26, 9, true, true, false, false);
        if (_platinumHandle == INVALID_HANDLE) {
            Print("Error creating MACD Platinum indicator");
            return(INIT_FAILED);
        }
        
        _qmpFilterHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", PERIOD_CURRENT, 12, 26, 9, true, 1, 8, 3, false, false);
        if (_qmpFilterHandle == INVALID_HANDLE) {
            Print("Error creating QMP Filter indicator");
            return(INIT_FAILED);
        }

        _longTermTimeFrameHandle = iMA(_Symbol, PERIOD_D1, _inpDailyMAPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_longTermTimeFrameHandle == INVALID_HANDLE) {
            Print("Error creating long term timeframe indicator");
            return(INIT_FAILED);
        }

        _longTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 240, 0, MODE_LWMA, PRICE_CLOSE);
        if (_longTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating long term MA indicator");
            return(INIT_FAILED);
        }

        _mediumTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 100, 0, MODE_EMA, PRICE_CLOSE);
        if (_mediumTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating medium term MA indicator");
            return(INIT_FAILED);
        }

        _shortTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 50, 0, MODE_EMA, PRICE_CLOSE);
        if (_shortTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating short term MA indicator");
            return(INIT_FAILED);
        }

        _shortTermATRHandle = iATR(_Symbol, PERIOD_CURRENT, _inpShortTermPeriod);
        if (_shortTermATRHandle == INVALID_HANDLE) {
            Print("Error creating short term ATR indicator");
            return(INIT_FAILED);
        }

        _longTermATRHandle = iATR(_Symbol, PERIOD_D1, _inpDailyMAPeriod);
        if (_longTermATRHandle == INVALID_HANDLE) {
            Print("Error creating long term ATR indicator");
            return(INIT_FAILED);
        }

        _longTermRsiHandle = iRSI(_Symbol, PERIOD_D1, 14, PRICE_CLOSE);
        if (_longTermRsiHandle == INVALID_HANDLE) {
            Print("Error creating long term RSI indicator");
            return(INIT_FAILED);
        }
        
        if (_inpShortTermTrendRejectionMultiplier < 0.5 || _inpShortTermTrendRejectionMultiplier > 4) {
            Print("Invalid value for inpShortTermTrendRejectionMultiplier: must be between 0.5 and 4");
            return(INIT_FAILED);
        }

        if (!(_inpStrongTrendThreshold > _inpStandardTrendThreshold && _inpStandardTrendThreshold > _inpWeakTrendThreshold && _inpWeakTrendThreshold > 0)) {
            Print("Invalid value(s) for trend thresholds");
            return(INIT_FAILED);
        }

        if (_inpSwingHighLowTPPips <= 0) {
            Print("Invalid value for inpSwingHighLowTPPips: must be >= 0");
            return(INIT_FAILED);
        }

        if (_inpSwingHighLowTrailStopPips < 0 || _inpSwingHighLowTrailStopPips > 10) {
            Print("Invalid value for _inpSwingHighLowTrailStopPips: must be between 0 and 10");
            return(INIT_FAILED);

        }

        if (!InitWritingFile()) {
            return(INIT_FAILED);
        }

        _upIndex = -1;
        _downIndex = -1;
    }

    return retCode;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    FileClose(_fileHandle);

    Print("Releasing indicator handles");
    
    ReleaseIndicator(_qmpFilterHandle);
    ReleaseIndicator(_platinumHandle);
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_mediumTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);
    ReleaseIndicator(_longTermTimeFrameHandle);    
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

    CheckTrend();

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
            HandleMartingale();
            StorePerfData();
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

bool InitWritingFile()
{
    string fileName = "WaveCatcher-" + Symbol() + " " + IntegerToString(PeriodSeconds() / 60) + ".csv";
    _fileHandle = FileOpen(fileName, FILE_WRITE | FILE_ANSI | FILE_CSV, ",");
    if (_fileHandle == INVALID_HANDLE) {
        Alert("Error opening file for writing");
        return false;
    }

    Print("File name: ", fileName);

    FileWrite(_fileHandle, "Deal", "Entry Time", "S/L", "Entry", "Exit Time", "Exit", "Profit",
        "Open", "High", "Low", "Close",
        "MA50", "MA240",
        "Signal",
        "MACD",
        "Up Idx", "Dn Idx",
        "High 20", "High 25",
        "D Trend",
        "RSI Current", "RSI Prior",
        "D MA1", "D MA2");

    return true;
}

void HandleMartingale()
{
    if (!_inpUseMartingale) return;

    // Did we have a recently closed position?
    printf("bars since position closed: %f, losing trade count: %f", _barsSincePositionClosed, _losingTradeCount);

    if (_barsSincePositionClosed <= _inpMartingalePeriod) {
        if (_losingTradeCount <= 0) {
            return;
        }

        int risk = GetFibSeriesNumber(_losingTradeCount + 2);
        double newRisk = _inpDynamicSizingRiskPerTrade * risk;
        printf("Increasing risk to %f%% now that we have had %d successive losing trades", newRisk, _losingTradeCount);

        _fixedRisk.Percent(newRisk);
        _martingaleActive = true;
    }
    else {
        Print("Resetting losing trade counter as it's now been too long ago since the last position was closed.");
        _losingTradeCount = 0;
        return;
    }
}

int GetFibSeriesNumber(int n)
{
    int i, t1 = 0, t2 = 1, nextTerm;

    for (i = 1; i <= n; ++i)
    {
        nextTerm = t1 + t2;
        t1 = t2;
        t2 = nextTerm;
    }

    return t1;
}

void CheckTrend()
{
    // TODO: Change back to 2 once we are finished with perf data
    int count = CopyBuffer(_qmpFilterHandle, 0, 0, 20, _qmpFilterUpData);
    if (count <= 0) {
        Print("Error copying QMP Filter data for up buffer.");
        return;
    }

    count = CopyBuffer(_qmpFilterHandle, 1, 0, 20, _qmpFilterDownData);
    if (count <= 0) {
        Print("Error copying QMP Filter data for down buffer.");
        return;
    }

    // Only needed for perf data
    count = CopyBuffer(_platinumHandle, 0, 0, 26, _macdData);
    if (count <= 0) {
        Print("Error copying MACD data.");
        return;
    }

    // Only needed for perf data
    count = CopyBuffer(_platinumHandle, 0, 0, 26, _macdData);
    if (count <= 0) {
        Print("Error copying MACD data.");
        return;
    }

    // Check for a signal
    string trend = GetTrendDirection(1);
    if (trend == "Dn") {
        _currentSignal = "Dn";
    }
    else if (_currentSignal == "Dn" && trend == "Up") {
        _currentSignal = "Up";
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
        _currentSignal = "";
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

    if (_inpUseMartingale) {
        if (LastTradeMadeProfit()) {
            _losingTradeCount = 0;
        }
        else {
            _losingTradeCount++;
        }
    }

    if (reset) {     
        WritePerformanceToFile();
        ResetState();        
    }
}

bool LastTradeMadeProfit()
{
    int days = 14; // Assume a position is never held longer than 14 days
    int minutes = 60 * 24 * days;
    datetime to = TimeCurrent();
    datetime from = to - 60 * minutes;

    if (!HistorySelect(from, to)) {
        Print("Failed to retrieve history for deals");
    }

    int dealsTotal = HistoryDealsTotal();
    if (dealsTotal <= 0) {
        Print("No deals found");
        return false;
    }

    for (int dealIndex = dealsTotal - 1; dealIndex >= 0; dealIndex--) {
        ulong inDeal = HistoryDealGetTicket(dealIndex);

        // type of entry
        long dealEntry = HistoryDealGetInteger(inDeal, DEAL_ENTRY);
        if (dealEntry != DEAL_ENTRY_IN) {
            continue;
        }

        string inSymbol = HistoryDealGetString(inDeal, DEAL_SYMBOL);
        datetime entryTime = (datetime)HistoryDealGetInteger(inDeal, DEAL_TIME);

        // Find the corresponding out deal
        bool foundExit = false;
        ulong outDeal = 0;

        for (int outDealIndex = dealIndex + 1; outDealIndex < dealsTotal; outDealIndex++) {
            outDeal = HistoryDealGetTicket(outDealIndex);
            long exitDealNumber = HistoryDealGetInteger(outDeal, DEAL_TICKET);
            dealEntry = HistoryDealGetInteger(outDeal, DEAL_ENTRY);
            if (dealEntry == DEAL_ENTRY_OUT) {
                string outSymbol = HistoryDealGetString(outDeal, DEAL_SYMBOL);
                if (inSymbol == outSymbol) {
                    foundExit = true;
                    break;
                }
            }
        }

        double profit = 0;
        if (foundExit) {
            profit = HistoryDealGetDouble(outDeal, DEAL_PROFIT);
            Print("Profit from last trade was ", profit);
            return (profit > 0);
        } else {
            Print("Couldn't determine profit from last trade");
        }
    }

    return false;
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

    if (inpInitialStopLossRule != StaticPipsValue && inpInitialStopLossRule != CurrentBarNPips && inpInitialStopLossPips != 0) {
        Print("Invalid initial stop loss rule.  Pips should be 0 when not using StaticPipsValue or CurrentBarNPips - init failed.");
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
    _fixedRisk.Percent(_inpDynamicSizingRiskPerTrade);
    _currentSignal = "";
    _recentHigh = 0;
    _recentLow = 999999;
    _alreadyMovedToBreakEven = false;
    _recentSwingHigh = 0;
    _recentSwingLow = 0;
    _hadRecentSwingHigh = false;
    _hadRecentSwingLow = false;
    _initialStop = 0;
    _barsSincePositionClosed = 0;
    _martingaleActive = false;
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

    count = CopyBuffer(_mediumTermTrendHandle, 0, 0, 2, _mediumTermTrendData);
    if (count <= 0) {
        Print("Error copying medium term trend data.");
        return;
    }

    count = CopyBuffer(_shortTermTrendHandle, 0, 0, _inpShortTermPeriod, _shortTermTrendData);
    if (count <= 0) {
        Print("Error copying short term trend data.");
        return;
    }

    count = CopyBuffer(_longTermTimeFrameHandle, 0, 0, _inpDailyMAPeriod, _longTermTimeFrameData);
    if (count <= 0) {
        Print("Error copying long term timeframe data.");
        return;
    }

    count = CopyBuffer(_shortTermATRHandle, 0, 0, _inpShortTermPeriod, _shortTermATRData);
    if (count <= 0) {
        Print("Error copying short term ATR data.");
        return;
    }

    count = CopyBuffer(_longTermATRHandle, 0, 0, _inpDailyMAPeriod, _longTermATRData);
    if (count <= 0) {
        Print("Error copying long term ATR data.");
        return;
    }

    count = CopyBuffer(_longTermRsiHandle, 0, 0, 14, _longTermRsiData);
    if (count <= 0) {
        Print("Error copying long term RSI data.");
        return;
    }
}

bool CheckToModifyPositions()
{
    if (!_position.Select(Symbol())) {
        return false;
    }

    if (_position.PositionType() == POSITION_TYPE_BUY) {
        if (LongModified())
            return true;
    }
    else {
        if (ShortModified())
            return true;
    }

    return false;
}

bool LongModified()
{
    double newStop = 0;
    double breakEvenPoint = _position.PriceOpen() * 2 - _initialStop;
    double doubleRiskReward = _position.PriceOpen() + 2 * (_position.PriceOpen() - _initialStop);
    double takeProfit = _position.TakeProfit();

    // Check if we have got a bearish red signal
    if (_currentSignal == "Dn") {
        if (_currentAsk <= doubleRiskReward) {
            printf("Moving SL to %d pips below low of last bar", _inpTrailAfterGoodProfitPips);
            newStop = _prices[1].low - _adjustedPoints * _inpTrailAfterGoodProfitPips;
            _currentSignal = "";
        }
    }
    else {
        if (!_alreadyMovedToBreakEven) {
            if (_currentAsk <= breakEvenPoint) {
                // Nothing to do
                return false;
            }
        }
        else {            
            if (_currentAsk <= doubleRiskReward) {
                return false;
            }

            // We've already reached double risk/reward ratio - check that we're hitting new highs and look for the next swing high

            // Are we making higher highs?
            if (_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh) {
                _recentHigh = _prices[1].high;
                _recentSwingHigh = _prices[1].low;
                _hadRecentSwingHigh = false;
            }

            // Filter on _barsSincePositionOpened to give the position time to "breathe" (i.e. avoid moving SL too early after initial SL)
            if (!_hadRecentSwingHigh) {
                // For this SL rule we only operate after a new bar forms
                //if (IsNewBar(iTime(0))) {
                if (!(_isNewBar && _barsSincePositionOpened >= 3)) {
                    return false;
                }

                if (_prices[1].close < _recentSwingHigh && _prices[1].high < _recentHigh) {
                    Print("Swing high found: ", _recentHigh);

                    // We have a swing high.  Set SL to the low of the swing high bar plus a margin
                    newStop = _prices[1].low - _adjustedPoints * _inpSwingHighLowTrailStopPips;
                    takeProfit = _prices[1].high + _inpSwingHighLowTPPips;
                    _hadRecentSwingHigh = true;
                }
                else {
                    // No new swing high - nothing to do
                    return false;
                }
            }
            else {
                // We've had a recent swing high so the stop loss has already been moved
                return false;
            }
        }
    }
        
    // Check if we should move to breakeven
    if (!_alreadyMovedToBreakEven) {
        if (_currentAsk > breakEvenPoint) {
            if (newStop == 0.0 || breakEvenPoint > newStop) {
                printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);

                // Changing this so we don't actually move the SL

                /* This has changed quite a bit recently.  Historically, we would always move the stop to breakeven.
                   Then this was removed so we don't move the stop
                   AND NOW...we move only if Martingale is active, meaning we have increased our risk beyond normal.
                   This is a way to recover our losses quickly and manage the risk a little better.
                */
                //if (_martingaleActive) {
                //    //newStop = _position.PriceOpen();

                //    newStop = breakEvenPoint;
                //}
                
                _alreadyMovedToBreakEven = true;
            }
        }
    }

    if (newStop == 0.0) {
        return false;
    }
    
    return ModifyLongPosition(newStop, takeProfit);
}

bool ShortModified()
{
    double newStop = 0;
    double breakEvenPoint = _position.PriceOpen() * 2 - _initialStop;
    double doubleRiskReward = _position.PriceOpen() - 2 * (_initialStop - _position.PriceOpen());
    double takeProfit = _position.TakeProfit();

    // Check if we have got a bullish green signal
    if (_currentSignal == "Up") {
        if (_currentBid >= doubleRiskReward) {
            printf("Moving SL to %d pips above high of last bar", _inpTrailAfterGoodProfitPips);
            newStop = _prices[1].high + _adjustedPoints * _inpTrailAfterGoodProfitPips;
            _currentSignal = "";
        }
    }
    else {
        if (!_alreadyMovedToBreakEven) {
            if (_currentBid >= breakEvenPoint) {
                // Nothing to do
                return false;
            }
        }
        else {
            if (_currentBid >= doubleRiskReward) {
                return false;
            }

            // We've already reached double risk/reward ratio - check that we're hitting new lows and look for the next swing low

            // Are we making lower lows?
            if (_prices[1].low < _prices[2].low && _prices[1].low < _recentLow) {
                _recentLow = _prices[1].low;
                _recentSwingLow = _prices[1].high;
                _hadRecentSwingLow = false;
            }

            // Filter on _barsSincePositionOpened to give the position time to "breathe" (i.e. avoid moving SL too early after initial SL)
            if (!_hadRecentSwingLow) {
                // For this SL rule we only operate after a new bar forms
                //if (IsNewBar(iTime(0))) {
                if (!(_isNewBar && _barsSincePositionOpened >= 3)) {
                    return false;
                }

                if (_prices[1].close > _recentSwingLow && _prices[1].low > _recentLow) {
                    Print("Swing low found: ", _recentLow);

                    // We have a swing low.  Set SL to the high of the swing low bar plus a margin
                    newStop = _prices[1].high + _adjustedPoints * _inpSwingHighLowTrailStopPips;
                    takeProfit = _prices[1].low - _inpSwingHighLowTPPips;
                    _hadRecentSwingLow = true;
                }
                else {
                    // No new swing low - nothing to do
                    return false;
                }
            }
            else {
                // We've had a recent swing low so the stop loss has already been moved
                return false;
            }
        }
    }

    // Check if we should move to breakeven
    if (!_alreadyMovedToBreakEven) {
        if (_currentBid < breakEvenPoint) {
            if (newStop == 0.0 || breakEvenPoint < newStop) {
                printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);

                // Changing this so we don't actually move the SL

                /* This has changed quite a bit recently.  Historically, we would always move the stop to breakeven.
                Then this was removed so we don't move the stop
                AND NOW...we move only if Martingale is active, meaning we have increased our risk beyond normal.
                This is a way to recover our losses quickly and manage the risk a little better.
                */
                //if (_martingaleActive) {
                //    //newStop = _position.PriceOpen();

                //    newStop = breakEvenPoint;
                //}

                _alreadyMovedToBreakEven = true;
            }
        }
    }

    if (newStop == 0.0) {
        return false;
    }

    return ModifyShortPosition(newStop, takeProfit);
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
    Rule 1 - We must have closed near the high (i.e. had an up bar)
    Rule 2 - The slope of the long-term trend must be up
    Rule 3 - The current low must be higher than long-term MA (or maybe within 6 pips?)
    Rule 4 - The current low must be less than or equal to the short-term MA or within a pip or two (or perhaps at least one recent bar's low was lower)
    Rule 5 - The current close is higher than the short-term MA
    Rule 6 - The slope of the short-term MA must be flat or rising
    Rule 7 - There hasn't been a recent (in the last 15 or 20 bars) highest high that is more than a certain level above the current close (using ATR)
    */

    /*
    Second case scenario...

    Special very bullish scenario (not based on QMP filter signal)
    Low < all 3 15M MAs
    High > all 3 15M MAs
    Slope of short-term 15M MA is flat or rising
    */

    /*
    TREND_TYPE values:

    TREND_TYPE_HARD_DOWN = 0,   // strong down trend
    TREND_TYPE_DOWN = 1,        // down trend
    TREND_TYPE_SOFT_DOWN = 2,   // weak down trend
    TREND_TYPE_FLAT = 3,        // no trend
    TREND_TYPE_SOFT_UP = 4,     // weak up trend
    TREND_TYPE_UP = 5,          // up trend
    TREND_TYPE_HARD_UP = 6      // strong up trend
    */
    if (_prices[1].high - _prices[1].close > _prices[1].close - _prices[1].low) return false;

    if (_prices[1].low < _shortTermTrendData[1] &&
        _prices[1].open < _shortTermTrendData[1] &&
        _prices[1].low < _longTermTrendData[1] &&
        _prices[1].low < _mediumTermTrendData[1] &&
        _prices[1].high > _shortTermTrendData[1] &&
        _prices[1].close > _shortTermTrendData[1] &&
        _prices[1].high > _mediumTermTrendData[1] &&
        _prices[1].close > _longTermTrendData[1])
    {
        TREND_TYPE longTrend = LongTermTrend();
        Print("For special case, Long term trend determined to be: ", GetTrendDescription(longTrend));
        switch (longTrend)
        {
            case TREND_TYPE_UP:
                // Fall-through
            case TREND_TYPE_HARD_UP:
                // Fall-through
            case TREND_TYPE_SOFT_UP:
                // Fall-through
            case TREND_TYPE_FLAT:
                break;

            default:
                return false;
        }

        printf("H=%f,L=%f,C=%f", _prices[1].high, _prices[1].low, _prices[1].close);

        return true;
    }

    return false;

    //if (_prices[1].low < _longTermTrendData[1]) return false;
    //if (_prices[1].low > _shortTermTrendData[1]) return false;
    //if (_prices[1].close <= _shortTermTrendData[1]) return false;

    //TREND_TYPE longTrend = LongTermTrend();
    //Print("Long term trend determined to be: ", GetTrendDescription(longTrend));
    //switch (longTrend)
    //{
    //case TREND_TYPE_UP:
    //    // Fall-through
    //case TREND_TYPE_HARD_UP:
    //    break;

    //default:
    //    return false;
    //}

    //Print("Checking short-term trend");
    //TREND_TYPE shortTrend = ShortTermTrend();
    //Print("Short term trend determined to be: ", GetTrendDescription(shortTrend));
    //switch (shortTrend)
    //{
    //case TREND_TYPE_FLAT:
    //    // Fall-through
    //case TREND_TYPE_SOFT_UP:
    //    // Fall-through
    //case TREND_TYPE_UP:
    //    // Fall-through
    //case TREND_TYPE_HARD_UP:
    //    break;

    //default:
    //    return false;
    //}

    //if (HadRecentHigh()) {
    //    return false;
    //}

    //return true;
}

bool HasBearishSignal()
{
    /*
    Rule 1 - We must have closed near the low (i.e. had a down bar)
    Rule 2 - The slope of the long-term trend must be down
    Rule 3 - The current high must be lower than the long-term MA (or maybe within 6 pips?)
    Rule 4 - The current high must be greater than or equal to the short-term MA or within a pip or two (or perhaps at least one recent bar's low was lower)
    Rule 5 - The current close must be lower than the short-term MA
    Rule 6 - The slope of the short-term MA is flat or down
    Rule 7 - There hasn't been a recent (in the last 15 bars) lowest low that is more than a certain level below the current close (using ATR)
    */

    if (_prices[1].high - _prices[1].close < _prices[1].close - _prices[1].low) return false;
    if (_prices[1].high > _longTermTrendData[1]) return false;
    if (_prices[1].high < _shortTermTrendData[1]) return false;
    if (_prices[1].close >= _shortTermTrendData[1]) return false;

    TREND_TYPE longTrend = LongTermTrend();
    Print("Long term trend determined to be: ", GetTrendDescription(longTrend));
    switch (longTrend)
    {
        case TREND_TYPE_DOWN:
            // Fall-through
        case TREND_TYPE_HARD_DOWN:
            break;

        default:
            return false;
    }

    Print("Checking short-term trend");
    TREND_TYPE shortTrend = ShortTermTrend();
    Print("Short term trend determined to be: ", GetTrendDescription(shortTrend));
    switch (shortTrend)
    {
        case TREND_TYPE_FLAT:
            // Fall-through
        case TREND_TYPE_SOFT_DOWN:
            // Fall-through
        case TREND_TYPE_DOWN:
            // Fall-through
        case TREND_TYPE_HARD_DOWN:
            break;

        default:
            return false;
    }

    //if (HadRecentLow()) {
    //    return false;
    //}

    return true;
}

TREND_TYPE LongTermTrend()
{
    //double recentATR = _longTermATRData[0];
    //double priorATR = _longTermATRData[_inpLongTermPeriod - 1];    
    double recentATR = _shortTermATRData[0];
    double priorATR = _shortTermATRData[_inpShortTermPeriod - 1];

    //TREND_TYPE trend = Trend(_longTermTimeFrameData[1], _longTermTimeFrameData[_inpLongTermPeriod - 1], recentATR, priorATR);
    TREND_TYPE trend = Trend(_longTermTrendData[1], _longTermTrendData[_inpLongTermPeriod - 1], recentATR, priorATR);
    return trend;
}

TREND_TYPE ShortTermTrend()
{
    double recentATR = _shortTermATRData[0];
    double priorATR = _shortTermATRData[_inpShortTermPeriod - 1];

    TREND_TYPE trend = Trend(_shortTermTrendData[1], _shortTermTrendData[_inpShortTermPeriod - 1], recentATR, priorATR);
    return trend;
}

TREND_TYPE Trend(double recentValue, double priorValue, double recentATR, double priorATR)
{
    double diff = recentValue - priorValue;
    double atr = (recentATR + priorATR) / 2;

    // Convert to pips
    //=IF(C27="L",(J27-D27)*Y27,(D27-J27)*Y27)
    //=IF(IFERROR(FIND("JPY",B27),-1) = -1, 10000, 100)

    /*
    int multiplier = 10000;
    if (StringFind(_Symbol, "JPY") > -1) {
    multiplier = 100;
    }

    double pips = diff * multiplier;
    */

    TREND_TYPE trend = TREND_TYPE_FLAT;

    /*
    TREND_TYPE_HARD_DOWN = 0,   // strong down trend
    TREND_TYPE_DOWN = 1,        // down trend
    TREND_TYPE_SOFT_DOWN = 2,   // weak down trend
    TREND_TYPE_FLAT = 3,        // no trend
    TREND_TYPE_SOFT_UP = 4,     // weak up trend
    TREND_TYPE_UP = 5,          // up trend
    TREND_TYPE_HARD_UP = 6      // strong up trend
    */

    /* 1.24449, 1.22.  ATR = 41 - 43.  Diff = 0.02449 (244.9 pips).  Average ATR = 42 pips.  244.9 / 42 = 5.83

    1.18021, 1.11897.  ATR = 35 - 39 pips.  Diff = 0.06124 (612.4 pips). Average ATR = 37 pips.  612.4 / 37 = 16.55

    1.123155, 1.110471.  ATR = 37 - 19.5.  Diff = 0.012684 (126.8 pips). Average ATR = 28 pips.  126.8 / 18 = 7.04

    short-term (25 bar):

    1.11474, 1.11309, ATR = 0.00065 - 0.00178. Diff = 0.00165 (16.5 pips). Average ATR = 11.3 pips. 16.5 / 11.3 = 1.46
    */

    double ratio = diff / atr;

    Print("Recent = ", recentValue, ", Prior = ", priorValue, ", Diff = ", diff, ", recentATR = ", recentATR, ", priorATR = ", priorATR, ", ratio = ", ratio);

    if (ratio >= _inpStrongTrendThreshold)
    {
        trend = TREND_TYPE_HARD_UP;
    }
    else if (ratio >= _inpStandardTrendThreshold)
    {
        trend = TREND_TYPE_UP;
    }
    else if (ratio >= _inpWeakTrendThreshold)
    {
        trend = TREND_TYPE_SOFT_UP;
    }
    else if (ratio <= -_inpWeakTrendThreshold)
    {
        trend = TREND_TYPE_HARD_DOWN;
    }
    else if (ratio <= -_inpStandardTrendThreshold)
    {
        trend = TREND_TYPE_DOWN;
    }
    else if (ratio <= -_inpStrongTrendThreshold)
    {
        trend = TREND_TYPE_SOFT_DOWN;
    }

    return trend;
}

//bool HadRecentHigh()
//{
//    double highest = 0.0;
//
//    for (int i = 1; i < _inpShortTermPeriod; i++) {
//        if (_prices[i].high > highest) {
//            highest = _prices[i].high;
//        }
//    }
//
//    if (highest - _prices[1].close > _atrData[0] * _inpShortTermTrendRejectionMultiplier) {
//        Print("Signal rejected.  Had a recent high of ", highest);
//        return true;
//    }
//
//    return false;
//}

//bool HadRecentLow()
//{
//    double lowest = 100000.0;
//
//    for (int i = 1; i < _inpShortTermPeriod; i++) {
//        if (_prices[i].low < lowest) {
//            lowest = _prices[i].low;
//        }
//    }
//
//    if (_prices[1].close - lowest > _atrData[0] * _inpShortTermTrendRejectionMultiplier) {
//        Print("Signal rejected.  Had a recent low of ", lowest);
//        return true;
//    }
//
//    return false;
//}

string GetTrendDescription(TREND_TYPE trend)
{
    switch (trend)
    {
    case TREND_TYPE_HARD_DOWN:
        return "strong down trend";

    case TREND_TYPE_DOWN:
        return "down trend";

    case TREND_TYPE_SOFT_DOWN:
        return "weak down trend";

    case TREND_TYPE_FLAT:
        return "no trend";

    case TREND_TYPE_SOFT_UP:
        return "weak up trend";

    case TREND_TYPE_UP:
        return "up trend";

    case TREND_TYPE_HARD_UP:
        return "strong up trend";

    default:
        return "unknown";
    }
}

string GetTrendDirection(int index)
{
    string trend = "X";

    if (_qmpFilterUpData[index] && _qmpFilterUpData[index] != 0.0) {
        trend = "Up";
    }
    else if (_qmpFilterDownData[index] && _qmpFilterDownData[index] != 0.0) {
        trend = "Dn";
    }

    return trend;
}

void WritePerformanceToFile()
{
    /* Options to write:
    
        _currentSignal = "";
        _recentHigh = 0;
        _recentLow = 999999;
        _alreadyMovedToBreakEven = false;
        _recentSwingHigh = 0;
        _hadRecentSwingHigh = false;
        _initialStop = 0;
        _barsSincePositionClosed = 0;
        _martingaleActive = false;

        int _barsSincePositionOpened;       // Counter of the number of bars since a position was opened
        int _barsSincePositionClosed;       // Counter of the number of bars since a position was closed
        int _losingTradeCount = 0;          // Counter of the number of consecutive losing trades
        bool _martingaleActive = false;     // A flag to indicate whether we have increased our lot size beyond the default for the current trade

        _shortTermTrendData[1]


    */

    const int days = 10;
    const int minutesInHour = 60;
    const int hoursInDay = 24;

    int minutes = hoursInDay * minutesInHour * days;
    datetime to = TimeCurrent();
    datetime from = to - 60 * minutes;

    if (!HistorySelect(from, to)) {
        Print("Failed to retrieve order history");
    }

    int dealsTotal = HistoryDealsTotal();
    if (dealsTotal <= 0) {
        return;
    }

    ulong outDeal = HistoryDealGetTicket(dealsTotal - 1);
    ulong inDeal = HistoryDealGetTicket(dealsTotal - 2);
    long dealNumber = HistoryDealGetInteger(inDeal, DEAL_TICKET);

    Print("Deals total: ", dealsTotal, ", in deal: ", inDeal, ", Out deal: ", outDeal);

    // type of entry
    long dealEntry = HistoryDealGetInteger(inDeal, DEAL_ENTRY);
    if (dealEntry != DEAL_ENTRY_IN) {
        Alert("Deal direction was not in");
        return;
    }

    dealEntry = HistoryDealGetInteger(outDeal, DEAL_ENTRY);
    if (dealEntry != DEAL_ENTRY_OUT) {
        Alert("Deal direction was not out");
        return;
    }

    datetime entryTime = (datetime)HistoryDealGetInteger(inDeal, DEAL_TIME);
    datetime exitTime = (datetime)HistoryDealGetInteger(outDeal, DEAL_TIME);
    string inSymbol = HistoryDealGetString(inDeal, DEAL_SYMBOL);
    string outSymbol = HistoryDealGetString(outDeal, DEAL_SYMBOL);

    double entryPrice = HistoryDealGetDouble(inDeal, DEAL_PRICE);
    double exitPrice = HistoryDealGetDouble(outDeal, DEAL_PRICE);
    double profit = HistoryDealGetDouble(outDeal, DEAL_PROFIT);

    if (entryPrice && entryTime && inSymbol == Symbol() && outSymbol == Symbol())
    {
        long dealType = HistoryDealGetInteger(inDeal, DEAL_TYPE);
        string dealTypeString;

        if (dealType == DEAL_TYPE_BUY) {
            dealTypeString = "L";
        }
        else if (dealType == DEAL_TYPE_SELL) {
            dealTypeString = "S";
        }

        FileWrite(_fileHandle, dealNumber, entryTime, dealTypeString, entryPrice, exitTime, exitPrice, profit,
            _open, _high, _low, _close,
            _shortTermTrend,
            _longTermTrend,
            _signalOnEntry,
            _macd,
            _upIndex,
            _downIndex,
            _highestHigh20, _highestHigh25,
            _longTimeframeTrend,
            _longTermRsiDataCurrent,
            _longTermRsiDataPrior,
            _dailyMA0,
            _dailyMA1
        );

        FileFlush(_fileHandle);
    }
    else
    {
        Alert("Didn't write trade to performance file");
    }
}

TREND_TYPE LongTimeframeTrend()
{
    double recentATR = _longTermATRData[0];
    double priorATR = _longTermATRData[_inpDailyMAPeriod - 1];

    TREND_TYPE trend = Trend(_longTermTimeFrameData[1], _longTermTimeFrameData[_inpDailyMAPeriod - 1], recentATR, priorATR);
    return trend;
}

void StorePerfData()
{
    _dailyMA0 = _longTermTimeFrameData[0];
    _dailyMA1 = _longTermTimeFrameData[1];
    
    _open = _prices[1].open;
    _high = _prices[1].high;
    _low = _prices[1].low;    
    _close = _prices[1].close;

    _macd = _macdData[1];

    _shortTermTrend = _shortTermTrendData[1];
    _longTermTrend = _longTermTrendData[1];

    _upIndex = -1;
    _downIndex = -1;

    for (int index = 1; index < 20; index++) {
        if (_qmpFilterUpData[index] && _qmpFilterUpData[index] != 0.0) {
            _upIndex = index;
            break;
        }
    }

    for (int index = 1; index < 20; index++) {
        if (_qmpFilterDownData[index] && _qmpFilterDownData[index] != 0.0) {
            _downIndex = index;
            break;
        }
    }

    _longTermRsiDataCurrent = _longTermRsiData[1];
    _longTermRsiDataPrior = _longTermRsiData[13];;

    _longTimeframeTrend = LongTimeframeTrend();

    _signalOnEntry = _currentSignal;

    CalcHighestHighs();
}

void CalcHighestHighs()
{
    _highestHigh20 = _prices[1].high;
    _highestHigh25 = _highestHigh20;

    for (int index = 1; index < 25; index++) {
        double h = _prices[index + 1].high;
        if (index < 20) {
            if (h > _highestHigh20) {
                _highestHigh20 = h;
            }
        }
        else if (h > _highestHigh25) {
            _highestHigh25 = h;
        }
    }    
}