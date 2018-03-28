#include "CMyExpertBase.mqh"

class CJimBrownTrend : public CMyExpertBase
{
public:
    CJimBrownTrend(void);
    ~CJimBrownTrend(void);
    virtual int Init
    (
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = PreviousBar5Pips,
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
        ENUM_TIMEFRAMES inpLongTermTimeFrame = PERIOD_D1,
        int             inpLongTermPeriod = 9
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
    int _shortTermTrendHandle;
    double _platinumUpCrossData[];
    double _platinumDownCrossData[];
    double _macdData[];
    double _longTermTrendData[];
    double _shortTermTrendData[];
    double _qmpFilterUpData[];
    double _qmpFilterDownData[];
    double _longTermTimeFrameData[];
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

    void CheckToMoveLongPositionToBreakEven();
    void CheckToMoveShortPositionToBreakEven();
    string GetTrendDirection(int index);
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
    int             inpLongTermPeriod
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
        ArraySetAsSeries(_shortTermTrendData, true);
        ArraySetAsSeries(_longTermTimeFrameData, true);

        _platinumHandle = iCustom(_Symbol, PERIOD_CURRENT, "MACD_Platinum", inpFastPlatinum, inpSlowPlatinum, inpSmoothPlatinum, true, true, false, false);
        if (_platinumHandle == INVALID_HANDLE) {
            Print("Error creating MACD Platinum indicator");
        }

        _qmpFilterHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", PERIOD_CURRENT, inpFTF_SF, inpFTF_RSI_Period, inpFTF_WP, true, inpFTF_SF, inpFTF_RSI_Period, inpFTF_WP, false, false);
        if (_qmpFilterHandle == INVALID_HANDLE) {
            Print("Error creating QMP Filter indicator");
        }

        _longTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 240, 0, MODE_LWMA, PRICE_CLOSE);
        if (_longTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating long term MA indicator");
        }

        _shortTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 50, 0, MODE_EMA, PRICE_CLOSE);
        if (_shortTermTrendHandle == INVALID_HANDLE) {
            Print("Error creating short term MA indicator");
        }

        _longTermTimeFrameHandle = iMA(_Symbol, inpLongTermTimeFrame, inpLongTermPeriod, 0, MODE_EMA, PRICE_CLOSE);
        if (_longTermTimeFrameHandle == INVALID_HANDLE) {
            Print("Error creating long term timeframe indicator");
        }

        _inpSlowPlatinum = inpSlowPlatinum;
        _inpSmoothPlatinum = inpSmoothPlatinum;
        _inpFTF_RSI_Period = inpFTF_RSI_Period;
        _inpLongTermPeriod = inpLongTermPeriod;

        _trend = "X";
        _sig = "Start";
    }

    return retCode;
}

void CJimBrownTrend::Deinit(void)
{
    Print("In derived class CJimBrownTrend OnDeInit");
    CMyExpertBase::Deinit();

    Print("Releasing indicator handles");

    if (_platinumHandle == 0) return;

    ReleaseIndicator(_platinumHandle);
    ReleaseIndicator(_qmpFilterHandle);
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);
    ReleaseIndicator(_longTermTimeFrameHandle);
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

    //count = CopyBuffer(_longTermTrendHandle, 0, 0, 2, _longTermTrendData);
    //if (count == -1) {
    //    Print("Error copying long term trend data.");
    //    return;
    //}    

    //count = CopyBuffer(_shortTermTrendHandle, 0, 0, 2, _shortTermTrendData);
    //if (count == -1) {
    //    Print("Error copying short term trend data.");
    //    return;
    //}

    //count = CopyBuffer(_longTermTimeFrameHandle, 0, 0, _inpLongTermPeriod, _longTermTimeFrameData);
    //if (count == -1) {
    //    Print("Error copying long term timeframe data.");
    //    return;
    //}
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
    //if (_prices[1].close >= _longTermTrendData[1] && _prices[1].low <= _shortTermTrendData[1]) {        
    //    if (_prices[1].close < _longTermTimeFrameData[1]) {
    //        Print("Rejecting signal due to long-term trend.");
    //        return false;
    //    }

    //    return true;
    //}

    return true;

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

    return true;

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