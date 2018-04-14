#include "CMyExpertBase.mqh"

class CJimBrownTrend : public CMyExpertBase
{
public:
    CJimBrownTrend(void);
    ~CJimBrownTrend(void);
    virtual int Init
    (
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = CurrentBarNPips,
        int             inpStopLossPips = 0,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 60,
        STOPLOSS_RULE   inpTrailingStopLossRule = StaticPipsValue,
        int             inpTrailingStopPips = 20,
        bool            inpMoveToBreakEven = true,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        int             inpFastPlatinum = 12,
        int             inpSlowPlatinum = 26,
        int             inpSmoothPlatinum = 9,
        int             inpFTF_SF = 1,
        int             inpFTF_RSI_Period = 8,
        int             inpFTF_WP = 3,
        ENUM_TIMEFRAMES inpLongTermTimeFrame = PERIOD_H4,
        int             inpLongTermPeriod = 9,
        double          inpMovedTooFarMultiplier = 3
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual void              OnTrade(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions();
    virtual void OnRecentlyClosedTrade();
    virtual bool CheckToModifyPositions();

private:
    int _platinumHandle;
    int _qmpFilterHandle;
    int _longTermTrendHandle;
    int _mediumTermTrendHandle;
    int _shortTermTrendHandle;
    int _longTermRsiHandle;
    int _adxHandle;
    double _platinumUpCrossData[];
    double _platinumDownCrossData[];
    double _macdData[];
    double _longTermTrendData[];
    double _mediumTermTrendData[];
    double _shortTermTrendData[];
    double _qmpFilterUpData[];
    double _qmpFilterDownData[];
    double _longTermTimeFrameData[];
    double _longTermRsiData[];
    double _adxData[];
    int _inpFTF_RSI_Period;
    int _inpSmoothPlatinum;
    int _inpSlowPlatinum;
    string _trend;
    string _filter1;
    string _sig;
    int _longTermTimeFrameHandle;
    int _inpLongTermPeriod;
    double _recentHigh;                 // Tracking the most recent high for stop management
    double _recentLow;                  // Tracking the most recent low for stop management
    double _inpMovedTooFarMultiplier;

    // The following are used for optimisation / monitoring performance
    // "MA50", "MA100", "MA240", "MACD", "H4 MA", "H4 RSI"
    int _fileHandle;
    double _ma50Data[], _ma100Data[], _ma240Data[];
    double _ma50, _ma100, _ma240, _macd, _h4MA, _h4Rsi, _h4MA0, _h4Rsi0, _low, _high, _upCrossRecentPrice, _upCrossPriorPrice, _downCrossRecentPrice, _downCrossPriorPrice;
    double _upCrossRecentValue, _upCrossPriorValue, _downCrossRecentValue, _downCrossPriorValue;
    int _upCrossRecentIndex, _upCrossPriorIndex, _downCrossRecentIndex, _downCrossPriorIndex;
    double _adx;

    void CheckToMoveLongPositionToBreakEven();
    void CheckToMoveShortPositionToBreakEven();
    string GetTrendDirection(int index);
    void WritePerformanceToFile();
    void StorePerfData();
};

CJimBrownTrend::CJimBrownTrend(void)
{
}

CJimBrownTrend::~CJimBrownTrend(void)
{
}

int CJimBrownTrend::Init(
    double          inpLots,
    STOPLOSS_RULE   inpStopLossRule,
    int             inpStopLossPips,
    bool            inpUseTakeProfit,
    int             inpTakeProfitPips,
    STOPLOSS_RULE   inpTrailingStopLossRule,
    int             inpTrailingStopPips,
    bool            inpMoveToBreakEven,
    bool            inpGoLong,
    bool            inpGoShort,
    bool            inpAlertTerminalEnabled,
    bool            inpAlertEmailEnabled,
    int             inpMinutesToWaitAfterPositionClosed,
    int             inpMinTradingHour,
    int             inpMaxTradingHour,    
    int             inpFastPlatinum,
    int             inpSlowPlatinum,
    int             inpSmoothPlatinum,
    int             inpFTF_SF,
    int             inpFTF_RSI_Period,
    int             inpFTF_WP,
    ENUM_TIMEFRAMES inpLongTermTimeFrame,
    int             inpLongTermPeriod,
    double          inpMovedTooFarMultiplier
    )
{
    Print("In derived class CJimBrownTrend OnInit");

    // Non-base variables initialised here
    int retCode = CMyExpertBase::Init(inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit, 
        inpTakeProfitPips, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort, 
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed, 
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for Jim Brown's trend following EA");

        ArraySetAsSeries(_platinumUpCrossData, true);
        ArraySetAsSeries(_platinumDownCrossData, true);
        ArraySetAsSeries(_macdData, true);

        ArraySetAsSeries(_qmpFilterUpData, true);
        ArraySetAsSeries(_qmpFilterDownData, true);

        ArraySetAsSeries(_longTermTrendData, true);
        ArraySetAsSeries(_mediumTermTrendData, true);
        ArraySetAsSeries(_shortTermTrendData, true);
        ArraySetAsSeries(_longTermTimeFrameData, true);
        ArraySetAsSeries(_longTermRsiData, true);

        ArraySetAsSeries(_adxData, true);

        _platinumHandle = iCustom(_Symbol, PERIOD_CURRENT, "MACD_Platinum", inpFastPlatinum, inpSlowPlatinum, inpSmoothPlatinum, true, true, false, false);
        if (_platinumHandle == INVALID_HANDLE) {
            Print("Error creating MACD Platinum indicator");
            return(INIT_FAILED);
        }

        _qmpFilterHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", PERIOD_CURRENT, inpFTF_SF, inpFTF_RSI_Period, inpFTF_WP, true, inpFTF_SF, inpFTF_RSI_Period, inpFTF_WP, false, false);
        if (_qmpFilterHandle == INVALID_HANDLE) {
            Print("Error creating QMP Filter indicator");
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

        _longTermTimeFrameHandle = iMA(_Symbol, inpLongTermTimeFrame, inpLongTermPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_longTermTimeFrameHandle == INVALID_HANDLE) {
            Print("Error creating long term timeframe indicator");
            return(INIT_FAILED);
        }

        _longTermRsiHandle = iRSI(_Symbol, inpLongTermTimeFrame, 14, PRICE_CLOSE);
        if (_longTermRsiHandle == INVALID_HANDLE) {
            Print("Error creating long term RSI indicator");
            return(INIT_FAILED);
        }        

        _adxHandle = iADX(_Symbol, PERIOD_CURRENT, 14);
        if (_adxHandle == INVALID_HANDLE) {
            Print("Error creating ADX indicator");
            return(INIT_FAILED);
        }

        _inpSlowPlatinum = inpSlowPlatinum;
        _inpSmoothPlatinum = inpSmoothPlatinum;
        _inpFTF_RSI_Period = inpFTF_RSI_Period;
        _inpLongTermPeriod = inpLongTermPeriod;
        _inpMovedTooFarMultiplier = inpMovedTooFarMultiplier;

        _trend = "X";
        _sig = "Start";

        string FileName = Symbol() + " " + IntegerToString(PeriodSeconds() / 60) + ".csv";
        _fileHandle = FileOpen(FileName, FILE_WRITE | FILE_ANSI | FILE_CSV, ",");
        if (_fileHandle == INVALID_HANDLE) {
            Alert("Error opening file for writing");
            return(INIT_FAILED);
        }

        FileWrite(_fileHandle, "Deal", "Entry Time", "S/L", "Entry", "Exit Time", "Exit", "Profit", "MA50", "MA100", "MA240", "MACD", "H4 MA 0", "H4 RSI 0", "H4 MA 1", "H4 RSI 1",
            "Signal Bar Low", "Signal Bar High", "Up Cross Recent Index","Up Cross Prior Index", "Up Cross Recent Value", "Up Cross Prior Value", "Up Cross Recent Price", "Up Cross Prior Price",
            "Down Cross Recent Index", "Down Cross Prior Index", "Down Cross Recent Value", "Down Cross Prior Value", "Down Cross Recent Price", "Down Cross Prior Price", "ADX");
    }

    return retCode;
}

void CJimBrownTrend::Deinit(void)
{
    Print("In derived class CJimBrownTrend OnDeInit");
    CMyExpertBase::Deinit();

    Print("Releasing indicator handles");

    ReleaseIndicator(_platinumHandle);
    ReleaseIndicator(_qmpFilterHandle);
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_mediumTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);
    ReleaseIndicator(_longTermTimeFrameHandle);
    ReleaseIndicator(_adxHandle);

    FileClose(_fileHandle);
}

void CJimBrownTrend::Processing(void)
{
    CMyExpertBase::Processing();
}

bool CJimBrownTrend::CheckToModifyPositions()
{
    //--- we work only at the time of the birth of new bar    
    if (!IsNewBar(iTime(0))) return false;    

    if (!_position.Select(Symbol())) {
        Print("Couldn't select position");
        return false;
    }

    int count = CopyBuffer(_qmpFilterHandle, 0, 0, 2, _qmpFilterUpData);
    if (count == -1) {
        Print("Error copying QMP Filter data for up buffer.");
        return false;
    }

    count = CopyBuffer(_qmpFilterHandle, 1, 0, 2, _qmpFilterDownData);
    if (count == -1) {
        Print("Error copying QMP Filter data for down buffer.");
        return false;
    }

    ulong deviation = 5; // Number of points

    if (_position.PositionType() == POSITION_TYPE_BUY) {        
        if (GetTrendDirection(1) == "Dn") {
            Print("Closing long position");
            return _trade.PositionClose(Symbol(), deviation);
        }
        else {
            CheckToMoveLongPositionToBreakEven();
        }
    }
    else if (_position.PositionType() == POSITION_TYPE_SELL) {
        if (GetTrendDirection(1) == "Up") {
            Print("Closing short position");
            return _trade.PositionClose(Symbol(), deviation);
        } 
        else {
            CheckToMoveShortPositionToBreakEven();
        }
    }

    return false;
}

void CJimBrownTrend::CheckToMoveLongPositionToBreakEven()
{
    if (!_inpMoveToBreakEven) {
        return;
    }

    if (!(_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh)) {
        return;
    }

    _recentHigh = _prices[1].high;

    if (_alreadyMovedToBreakEven) {
        return;
    }

    double breakEvenPoint = 0;
    double initialRisk = _position.PriceOpen() - _initialStop;
    breakEvenPoint = _position.PriceOpen() + initialRisk;

    if (_currentAsk <= breakEvenPoint) {
        return;
    }
    
    printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);
    double newStop = _position.PriceOpen();

    double sl = NormalizeDouble(newStop, _symbol.Digits());
    double tp = _position.TakeProfit();
    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

    if (_position.StopLoss() < sl || _position.StopLoss() == 0.0) {
        double diff = (_currentAsk - sl) / _adjustedPoints;
        if (diff < stopLevelPips) {
            printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
                _currentAsk, sl, stopLevelPips, diff);

            sl = _currentAsk - stopLevelPips * _adjustedPoints;
        }

        //--- modify position
        if (!_trade.PositionModify(Symbol(), sl, tp)) {
            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f", sl, tp);
        }

        _alreadyMovedToBreakEven = true;
    }

}

void CJimBrownTrend::CheckToMoveShortPositionToBreakEven()
{
    if (!_inpMoveToBreakEven) {
        return;
    }

    if (!(_prices[1].low < _prices[2].low && _prices[1].low < _recentLow)) {
        return;
    }

    _recentLow = _prices[1].low;

    if (_alreadyMovedToBreakEven) {
        return;
    }

    double breakEvenPoint = 0;
    double initialRisk = _initialStop - _position.PriceOpen();
    breakEvenPoint = _position.PriceOpen() - initialRisk;

    if (_currentAsk > breakEvenPoint) {
        return;
    }

    printf("Moving to breakeven now that the price has reached %f", breakEvenPoint);
    double newStop = _position.PriceOpen();

    double sl = NormalizeDouble(newStop, _symbol.Digits());
    double tp = _position.TakeProfit();
    double stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel

    if (_position.StopLoss() > sl || _position.StopLoss() == 0.0) {
        double diff = (sl - _currentBid) / _adjustedPoints;
        if (diff < stopLevelPips) {
            printf("Can't set new stop that close to the current price.  Ask = %f, new stop = %f, stop level = %f, diff = %f",
                _currentAsk, sl, stopLevelPips, diff);

            sl = _currentBid + stopLevelPips * _adjustedPoints;
        }

        //--- modify position
        if (!_trade.PositionModify(Symbol(), sl, tp)) {
            printf("Error modifying position for %s : '%s'", Symbol(), _trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f", sl, tp);
        }

        _alreadyMovedToBreakEven = true;
    }

}

void CJimBrownTrend::NewBarAndNoCurrentPositions()
{
    int count = CopyBuffer(_platinumHandle, 0, 0, _inpSlowPlatinum, _macdData);
    if (count == -1) {
        Print("Error copying MACD data.");
        return;
    }

    count = CopyBuffer(_platinumHandle, 2, 0, _inpSlowPlatinum, _platinumUpCrossData);
    if (count == -1) {
        Print("Error copying platinum up cross data.");
        return;
    }

    count = CopyBuffer(_platinumHandle, 3, 0, _inpSlowPlatinum, _platinumDownCrossData);
    if (count == -1) {
        Print("Error copying platinum down cross data.");
        return;
    }

    count = CopyBuffer(_qmpFilterHandle, 0, 0, 2, _qmpFilterUpData);
    if (count == -1) {
        Print("Error copying QMP Filter data for up buffer.");
        return;
    }

    count = CopyBuffer(_qmpFilterHandle, 1, 0, 2, _qmpFilterDownData);
    if (count == -1) {
        Print("Error copying QMP Filter data for down buffer.");
        return;
    }

    count = CopyBuffer(_longTermTrendHandle, 0, 0, 2, _longTermTrendData);
    if (count == -1) {
        Print("Error copying long term trend data.");
        return;
    }

    count = CopyBuffer(_mediumTermTrendHandle, 0, 0, 2, _mediumTermTrendData);
    if (count == -1) {
        Print("Error copying medium term trend data.");
        return;
    }

    count = CopyBuffer(_shortTermTrendHandle, 0, 0, 2, _shortTermTrendData);
    if (count == -1) {
        Print("Error copying short term trend data.");
        return;
    }

    count = CopyBuffer(_longTermTimeFrameHandle, 0, 0, _inpLongTermPeriod, _longTermTimeFrameData);
    if (count == -1) {
        Print("Error copying long term timeframe data.");
        return;
    }

    count = CopyBuffer(_longTermRsiHandle, 0, 0, 14, _longTermRsiData);
    if (count == -1) {
        Print("Error copying long term RSI data.");
        return;
    }

    count = CopyBuffer(_adxHandle, 0, 0, 14, _adxData);
    if (count == -1) {
        Print("Error copying ADX data.");
        return;
    }
}

void CJimBrownTrend::OnTrade(void)
{
    CMyExpertBase::OnTrade();
}

void CJimBrownTrend::OnRecentlyClosedTrade()
{
    Print("Resetting trend status");
    _trend = "X";
    _sig = "Start";

    _recentHigh = 0;
    _recentLow = 999999;
    _alreadyMovedToBreakEven = false;
    
    WritePerformanceToFile();
}

void CJimBrownTrend::WritePerformanceToFile()
{   
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

        FileWrite(_fileHandle, dealNumber, entryTime, dealTypeString, entryPrice, exitTime, exitPrice, profit, _ma50, _ma100, _ma240, _macd, _h4MA0, _h4Rsi0, _h4MA, _h4Rsi,
            _low, _high,
            _upCrossRecentIndex, _upCrossPriorIndex, _upCrossRecentValue, _upCrossPriorValue, _upCrossRecentPrice, _upCrossPriorPrice,
            _downCrossRecentIndex, _downCrossPriorIndex, _downCrossRecentValue, _downCrossPriorValue, _downCrossRecentPrice, _downCrossPriorPrice, _adx);
        FileFlush(_fileHandle);
    }
    else
    {
        Alert("Didn't write trade to performance file");
    }
}

bool CJimBrownTrend::HasBullishSignal()
{
    //CheckSignal();
    //bool basicSignal = _sig == "Buy";
    //
    //if (basicSignal) {
    if (GetTrendDirection(1) != "Up") {
        return false;
    }

    StorePerfData();

    if (_upCrossRecentIndex > -1 && _upCrossPriorIndex > -1 && _upCrossRecentValue > _upCrossPriorValue) {

        double range = _prices[1].high - _prices[1].low;
        if (range > _atrData[0] * _inpMovedTooFarMultiplier) {
            Print("Rejecting signal due to this being a large move so we assume it has been missed.");
            return false;
        }

        return true;
    }

    return false;

    /*int firstIndex = 0;
    int secondIndex = 0;

    for (int index = 0; index < _inpSlowPlatinum; index++) {
        if (_platinumUpCrossData[index] < 1) {
            if (firstIndex == 0) {
                firstIndex = index;
            }
            else {
                secondIndex = index;
                break;
            }
        }            
    }*/

    //// Ensure we have cross data and that the most recent one was recent
    //if (firstIndex == 0 || secondIndex == 0 || firstIndex > 3) {
    //    return false;
    //}
    ////    
    ////if (_platinumUpCrossData[firstIndex] > _platinumUpCrossData[secondIndex] &&
    ////    (_platinumUpCrossData[firstIndex] < 0 || _platinumUpCrossData[secondIndex] < 0)) {
    ////    Print("latest ma:", _platinumUpCrossData[firstIndex]);
    ////    Print("prior ma:", _platinumUpCrossData[secondIndex]);
    ////    return true;
    ////}
    //    
    //if (_platinumUpCrossData[firstIndex] > _platinumUpCrossData[secondIndex] &&
    //    _prices[firstIndex].close < _prices[secondIndex].close) {
    //    //Print("latest ma:", _platinumUpCrossData[firstIndex]);
    //    //Print("prior ma:", _platinumUpCrossData[secondIndex]);

    //    //Print("Price at recent bar ", firstIndex, " was ", _prices[firstIndex].close);
    //    //Print("Price at previous bar ", secondIndex, " was ", _prices[secondIndex].close);

    //    return true;
    //}

    /*if (_platinumUpCrossData[firstIndex] >= 0) {
        return true;
    }

    return false;       */
}

bool CJimBrownTrend::HasBearishSignal()
{
    //CheckSignal();
    //bool basicSignal = _sig == "Sell";
    //
    //if (basicSignal) {
    if (GetTrendDirection(1) != "Dn") {
        return false;
    }

    StorePerfData();

    if (_downCrossRecentIndex > -1 && _downCrossPriorIndex > -1 && _downCrossRecentValue < _downCrossPriorValue) {
        double range = _prices[1].high - _prices[1].low;
        if (range > _atrData[0] * _inpMovedTooFarMultiplier) {
            Print("Rejecting signal due to this being a large move so we assume it has been missed.");
            return false;
        }

        return true;
    }

    return false;
    //if (_prices[1].close <= _longTermTrendData[1] && _prices[1].high >= _shortTermTrendData[1]) {
    //    if (_prices[1].close > _longTermTimeFrameData[1]) {
    //        Print("Rejecting signal due to long-term trend.");
    //        return false;
    //    }

    //    return true;
    //}
    //int firstIndex = 0;
    //int secondIndex = 0;

    //for (int index = 0; index < _inpSlowPlatinum; index++) {
    //    if (_platinumDownCrossData[index] < 1) {
    //        if (firstIndex == 0) {
    //            firstIndex = index;
    //        }
    //        else {
    //            secondIndex = index;
    //            break;
    //        }
    //    }
    //}

    //// Ensure we have cross data and that the most recent one was recent
    //if (firstIndex == 0 || secondIndex == 0 || firstIndex > 3) {
    //    return false;
    //}

    //if (_platinumDownCrossData[firstIndex] < _platinumDownCrossData[secondIndex] &&
    //    _prices[firstIndex].close > _prices[secondIndex].close) {
    //    return true;
    //}

    //return false;
}

string CJimBrownTrend::GetTrendDirection(int index)
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

void CJimBrownTrend::StorePerfData()
{
    _ma50 = _shortTermTrendData[1];
    _ma100 = _mediumTermTrendData[1];
    _ma240 = _longTermTrendData[1];

    _macd = _macdData[1];
    _h4MA = _longTermTimeFrameData[1];
    _h4Rsi = _longTermRsiData[1];

    _h4MA0 = _longTermTimeFrameData[0];
    _h4Rsi0 = _longTermRsiData[0];

    _low = _prices[1].low;
    _high = _prices[1].high;

    _adx = _adxData[1];

    int upFirstIndex = -1;
    int upSecondIndex = -1;
    int downFirstIndex = -1;
    int downSecondIndex = -1;

    for (int index = 0; index < ArraySize(_platinumUpCrossData); index++) {
        if (_platinumUpCrossData[index] < 1) {
            if (upFirstIndex == -1) {
                upFirstIndex = index;
            }
            else {
                upSecondIndex = index;
            }
        }

        if (_platinumDownCrossData[index] < 1) {
            if (downFirstIndex == -1) {
                downFirstIndex = index;
            }
            else {
                downSecondIndex = index;
            }
        }
    }
    
    _upCrossRecentIndex = upFirstIndex;
    _upCrossPriorIndex = upSecondIndex;

    if (upFirstIndex == -1) {
        _upCrossRecentValue = 0;        
        _upCrossRecentPrice = 0;
    }
    else {
        _upCrossRecentValue = _platinumUpCrossData[upFirstIndex];
        _upCrossRecentPrice = _prices[upFirstIndex].close;
    }

    if (upSecondIndex == -1) {
        _upCrossPriorValue = 0;
        _upCrossPriorPrice = 0;
    }
    else {
        _upCrossPriorValue = _platinumUpCrossData[upSecondIndex];
        _upCrossPriorPrice = _prices[upSecondIndex].close;
    }

    _downCrossRecentIndex = downFirstIndex;
    _downCrossPriorIndex = downSecondIndex;

    if (downFirstIndex == -1) {
        _downCrossRecentValue = 0;
        _downCrossRecentPrice = 0;
    }
    else {
        _downCrossRecentValue = _platinumDownCrossData[downFirstIndex];
        _downCrossRecentPrice = _prices[downFirstIndex].close;
    }
    
    if (downSecondIndex == -1) {
        _downCrossPriorValue = 0;
        _downCrossPriorPrice = 0;
    }
    else {
        _downCrossPriorValue = _platinumDownCrossData[downSecondIndex];
        _downCrossPriorPrice = _prices[downSecondIndex].close;
    }       
}