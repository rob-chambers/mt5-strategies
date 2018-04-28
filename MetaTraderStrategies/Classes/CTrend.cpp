#include "CMyExpertBase.mqh"

class CTrend : public CMyExpertBase
{
public:
    CTrend(void);
    ~CTrend(void);
    virtual int Init
    (
        LOTSIZING_RULE  inpLotSizingRule = Dynamic,
        double          inpDynamicSizingRiskPerTrade = 1,
        double          inpLots = 0,
        STOPLOSS_RULE   inpStopLossRule = StaticPipsValue,
        int             inpStopLossPips = 15,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 30,
        double          inpTakeProfitRiskRewardRatio = 0,
        STOPLOSS_RULE   inpTrailingStopLossRule = StaticPipsValue,
        int             inpTrailingStopPips = 15,
        bool            inpMoveToBreakEven = true,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        int             inpLongTermPeriod = 70,
        int             inpShortTermPeriod = 25
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions(void);

private:
    int _inpLongTermPeriod;
    int _inpShortTermPeriod;
    int _longTermTimeFrameHandle;
    int _longTermTrendHandle;
    int _mediumTermTrendHandle;
    int _shortTermTrendHandle;
    int _shortTermATRHandle;
    int _longTermATRHandle;

    double _longTermTimeFrameData[];
    double _longTermTrendData[];
    double _mediumTermTrendData[];
    double _shortTermTrendData[];
    double _shortTermATRData[];
    double _longTermATRData[];

    double _recentHigh;                 // Tracking the most recent high for stop management
    double _recentLow;                  // Tracking the most recent low for stop management

    void CheckToMoveLongPositionToBreakEven();
    void CheckToMoveShortPositionToBreakEven();
    TREND_TYPE LongTermTrend();
    TREND_TYPE ShortTermTrend();
    TREND_TYPE Trend(double recentValue, double priorValue, double recentATR, double priorATR);
    string GetTrendDescription(TREND_TYPE trend);
    bool HadRecentHigh();
    bool HadRecentLow();
};

CTrend::CTrend(void)
{
}

CTrend::~CTrend(void)
{
}

int CTrend::Init(
    LOTSIZING_RULE  inpLotSizingRule,
    double          inpDynamicSizingRiskPerTrade,
    double          inpLots,
    STOPLOSS_RULE   inpStopLossRule,
    int             inpStopLossPips,
    bool            inpUseTakeProfit,
    int             inpTakeProfitPips,
    double          inpTakeProfitRiskRewardRatio,
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
    int             inpLongTermPeriod,
    int             inpShortTermPeriod
)
{
    Print("In derived class CTrend OnInit");

    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for trend EA");        

        ArraySetAsSeries(_longTermTimeFrameData, true);
        ArraySetAsSeries(_longTermTrendData, true);
        ArraySetAsSeries(_mediumTermTrendData, true);
        ArraySetAsSeries(_shortTermTrendData, true);
        ArraySetAsSeries(_shortTermATRData, true);
        ArraySetAsSeries(_longTermATRData, true);

        _longTermTimeFrameHandle = iMA(_Symbol, PERIOD_H4, 240, 0, MODE_LWMA, PRICE_CLOSE);
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

        _shortTermATRHandle = iATR(_Symbol, PERIOD_CURRENT, inpShortTermPeriod);
        if (_shortTermATRHandle == INVALID_HANDLE) {
            Print("Error creating short term ATR indicator");
            return(INIT_FAILED);
        }

        _longTermATRHandle = iATR(_Symbol, PERIOD_H4, inpLongTermPeriod);
        if (_longTermATRHandle == INVALID_HANDLE) {
            Print("Error creating long term ATR indicator");
            return(INIT_FAILED);
        }

        _inpLongTermPeriod = inpLongTermPeriod;
        _inpShortTermPeriod = inpShortTermPeriod;
    }

    return retCode;
}

void CTrend::Deinit(void)
{
    Print("In derived class CTrend OnDeInit");
    CMyExpertBase::Deinit();

    Print("Releasing indicator handles");

    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_mediumTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);
    ReleaseIndicator(_longTermTimeFrameHandle);
}

void CTrend::Processing(void)
{
    CMyExpertBase::Processing();
}

void CTrend::NewBarAndNoCurrentPositions(void)
{
    int count;

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

    count = CopyBuffer(_shortTermTrendHandle, 0, 0, _inpShortTermPeriod, _shortTermTrendData);
    if (count == -1) {
        Print("Error copying short term trend data.");
        return;
    }

    count = CopyBuffer(_longTermTimeFrameHandle, 0, 0, _inpLongTermPeriod, _longTermTimeFrameData);
    if (count == -1) {
        Print("Error copying long term timeframe data.");
        return;
    }

    count = CopyBuffer(_shortTermATRHandle, 0, 0, _inpShortTermPeriod, _shortTermATRData);
    if (count == -1) {
        Print("Error copying short term ATR data.");
        return;
    }

    count = CopyBuffer(_longTermATRHandle, 0, 0, _inpLongTermPeriod, _longTermATRData);
    if (count == -1) {
        Print("Error copying long term ATR data.");
        return;
    }
}

bool CTrend::HasBullishSignal()
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

    // Special case first
    if (_prices[1].low < _shortTermTrendData[1] &&
        _prices[1].low < _longTermTrendData[1] &&
        _prices[1].low < _mediumTermTrendData[1] &&
        _prices[1].high > _shortTermTrendData[1] &&
        _prices[1].high > _mediumTermTrendData[1] &&
        _prices[1].high > _longTermTrendData[1])
    {
        TREND_TYPE longTrend = LongTermTrend();
        Print("Long term trend determined to be: ", GetTrendDescription(longTrend));
        switch (longTrend)
        {
            case TREND_TYPE_UP:
                // Fall-through
            case TREND_TYPE_HARD_UP:
                // Fall-through
            case TREND_TYPE_SOFT_UP:
                break;

            default:
                return false;
        }

        return true;
    }

    if (_prices[1].low < _longTermTrendData[1]) return false;
    if (_prices[1].low > _shortTermTrendData[1]) return false;
    if (_prices[1].close <= _shortTermTrendData[1]) return false;

    TREND_TYPE longTrend = LongTermTrend();
    Print("Long term trend determined to be: ", GetTrendDescription(longTrend));
    switch (longTrend)
    {
        case TREND_TYPE_UP:
            // Fall-through
        case TREND_TYPE_HARD_UP:
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
        case TREND_TYPE_SOFT_UP:
            // Fall-through
        case TREND_TYPE_UP:
            // Fall-through
        case TREND_TYPE_HARD_UP:
            break;

        default:
            return false;
    }

    if (HadRecentHigh()) {
        return false;
    }

    return true;
}

bool CTrend::HasBearishSignal()
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

    if (HadRecentLow()) {
        return false;
    }

    return true;
}

void CTrend::CheckToMoveLongPositionToBreakEven()
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

void CTrend::CheckToMoveShortPositionToBreakEven()
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

TREND_TYPE CTrend::LongTermTrend() 
{
    double recentATR = _longTermATRData[0];
    double priorATR = _longTermATRData[_inpLongTermPeriod - 1];    

    TREND_TYPE trend = Trend(_longTermTimeFrameData[1], _longTermTimeFrameData[_inpLongTermPeriod - 1], recentATR, priorATR);
    return trend;
}

TREND_TYPE CTrend::ShortTermTrend()
{
    double recentATR = _shortTermATRData[0];
    double priorATR = _shortTermATRData[_inpShortTermPeriod - 1];

    TREND_TYPE trend = Trend(_shortTermTrendData[1], _shortTermTrendData[_inpShortTermPeriod - 1], recentATR, priorATR);
    return trend;
}

TREND_TYPE CTrend::Trend(double recentValue, double priorValue, double recentATR, double priorATR)
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

    if (ratio >= 3)
    {
        trend = TREND_TYPE_HARD_UP;
    }
    else if (ratio >= 1)
    {
        trend = TREND_TYPE_UP;
    }
    else if (ratio >= 0.5)
    {
        trend = TREND_TYPE_SOFT_UP;
    }
    else if (ratio <= -3)
    {
        trend = TREND_TYPE_HARD_DOWN;
    }
    else if (ratio <= -1)
    {
        trend = TREND_TYPE_DOWN;
    }
    else if (ratio <= -0.5)
    {
        trend = TREND_TYPE_SOFT_DOWN;
    }

    return trend;
}

bool CTrend::HadRecentHigh()
{
    double highest = 0.0;

    for (int i = 1; i < _inpShortTermPeriod; i++) {
        if (_prices[i].high > highest) {
            highest = _prices[i].high;
        }
    }

    if (highest - _prices[1].close > _atrData[0] * 1.5) {
        Print("Signal rejected.  Had a recent high of ", highest);
        return true;
    }

    return false;
}

bool CTrend::HadRecentLow()
{
    double lowest = 100000.0;

    for (int i = 1; i < _inpShortTermPeriod; i++) {
        if (_prices[i].low < lowest) {
            lowest = _prices[i].low;
        }
    }

    if (_prices[1].close - lowest > _atrData[0] * 1.5) {
        Print("Signal rejected.  Had a recent low of ", lowest);
        return true;
    }

    return false;
}

string CTrend::GetTrendDescription(TREND_TYPE trend)
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