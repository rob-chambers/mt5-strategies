#include "CMyExpertBase.mqh"

class CNewTrend : public CMyExpertBase
{
public:
    CNewTrend(void);
    ~CNewTrend(void);
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
        int             inpTrailingStopPips = 20,
        bool            inpMoveToBreakEven = true,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        int             inpBarCountHighestHigh = 40,
        bool            inpFilterByADX = true,
        int             inpADXPeriod = 14,
        int             inpBarCountInRange = 10,
        int             inpADXThreshold = 30,
        bool            inpFilterByMA = true,
        ENUM_TIMEFRAMES inpMAPeriodType = PERIOD_H1,
        int             inpMAPeriodAmount = 21
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions(void);

private:
    //double _inpPinbarThreshhold;
    //double _inpPinbarRangeThreshhold;
    int _highsHandle;
    double _highsData[];
    double _adxData[];

    int _inpBarCountHighestHigh;
    bool _inpFilterByADX;
    int _adxHandle;
    int _inpBarCountInRange;
    int _inpADXThreshold;

    bool _inpFilterByMA;
    ENUM_TIMEFRAMES _inpMAPeriodType;
    int _inpMAPeriodAmount;
    int _maHandle;
    double _maData[];

    bool IsHighestHigh();
    bool IsLowestLow();
    bool IsCloseNearHigh();
    bool IsCloseNearLow();
    bool IsRangeIncreasing();
    bool InRange();
};

CNewTrend::CNewTrend(void)
{
}

CNewTrend::~CNewTrend(void)
{
}

int CNewTrend::Init(
    LOTSIZING_RULE  inpLotSizingRule,
    double          inpDynamicSizingRiskPerTrade,
    double          inpLots,
    STOPLOSS_RULE   inpInitialStopLossRule,
    int             inpInitialStopLossPips,
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
    int             inpBarCountHighestHigh,
    bool            inpFilterByADX,
    int             inpADXPeriod,
    int             inpBarCountInRange,
    int             inpADXThreshold,
    bool            inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int             inpMAPeriodAmount
    )
{
    Print("In derived class CNewTrend OnInit");

    // Non-base variables initialised here
    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpInitialStopLossRule, inpInitialStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for new trend EA");
        ArraySetAsSeries(_adxData, true);

        _inpBarCountHighestHigh = inpBarCountHighestHigh;
        _inpFilterByADX = inpFilterByADX;
        _inpBarCountInRange = inpBarCountInRange;
        _inpADXThreshold = inpADXThreshold;

        _inpFilterByMA = inpFilterByMA;
        _inpMAPeriodType = inpMAPeriodType;
        _inpMAPeriodAmount = inpMAPeriodAmount;

        if (inpFilterByADX) {
            _adxHandle = iADX(Symbol(), PERIOD_CURRENT, inpADXPeriod);
        }

        if (inpFilterByMA) {
            ArraySetAsSeries(_maData, true);
            _maHandle = iMA(_Symbol, inpMAPeriodType, _inpMAPeriodAmount, 0, MODE_SMA, PRICE_CLOSE);
        }
    }

    return retCode;
}

void CNewTrend::Deinit(void)
{
    Print("In derived class CNewTrend OnDeInit");
    CMyExpertBase::Deinit();
    if (_adxHandle > 0) {
        Print("Releasing ADX indicator handle");
        ReleaseIndicator(_adxHandle);
    }

    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void CNewTrend::Processing(void)
{
    CMyExpertBase::Processing();
}

void CNewTrend::NewBarAndNoCurrentPositions(void)
{
    int highsDataCount = CopyBuffer(_highsHandle, 0, 0, 40, _highsData);
    if (_adxHandle > 0) {
        int adxDataCount = CopyBuffer(_adxHandle, 0, 0, _inpBarCountInRange, _adxData);
    }

    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool CNewTrend::HasBullishSignal()
{
    /* Rules:
    1) Close must be higher than open
    2) Current high > yesterday's high
    3) Current high must be higher than all highs for last x bars
    4) Optionally, close must be near the high
    5) Range must be increasing
    6) We must be in a trading range (i.e. not trending)
    */
    if (!(_prices[1].close > _prices[1].open)) return false;
    if (!(_prices[1].high > _prices[2].high)) return false;

    if (!IsHighestHigh()) return false;

    if (!IsCloseNearHigh()) return false;
    if (!IsRangeIncreasing()) return false;

    if (_inpFilterByADX && !InRange()) return false;

    //double closeFromHigh = _prices[1].high - _prices[1].close;
    //double openFromHigh = _prices[1].high - _prices[1].open;


    /*
    double currentRange = _prices[1].high - _prices[1].low;
    if (!((closeFromHigh / currentRange <= (1 - _inpPinbarThreshhold)) &&
        (openFromHigh / currentRange <= (1 - _inpPinbarThreshhold)))) {
        return false;
    }
    */

    if (_inpFilterByMA && _prices[1].close < _maData[0]) {
        return false;
    }

    /*
    bool maSignal = false;
    if (!_inpUseMA) {
        // Ignore if we don't care
        maSignal = true;
    }
    else {
        maSignal = _prices[1].close < _maData[0];
    }
    */

    /*
    double avg = (_prices[2].high - _prices[2].low + _prices[3].high - _prices[3].low + _prices[4].high - _prices[4].low) / 3;
    if (currentRange / _inpPinbarRangeThreshhold < avg) {
        return false;
    }
    */
    
    return true;
}

bool CNewTrend::HasBearishSignal()
{
    /* Rules:
    1) Close must be lower than open
    2) Current high < yesterday's high
    3) Current low must be lower than all lows for last x bars
    4) Optionally, close must be near the low
    5) Range must be increasing
    6) We must be in a trading range (i.e. not trending)
    */
    if (!(_prices[1].close < _prices[1].open)) return false;
    if (!(_prices[1].high < _prices[2].high)) return false;

    if (!IsLowestLow()) return false;

    if (!IsCloseNearLow()) return false;
    if (!IsRangeIncreasing()) return false;

    if (_inpFilterByADX && !InRange()) return false;

    //double closeFromHigh = _prices[1].high - _prices[1].close;
    //double openFromHigh = _prices[1].high - _prices[1].open;


    /*
    double currentRange = _prices[1].high - _prices[1].low;
    if (!((closeFromHigh / currentRange <= (1 - _inpPinbarThreshhold)) &&
    (openFromHigh / currentRange <= (1 - _inpPinbarThreshhold)))) {
    return false;
    }
    */

    if (_inpFilterByMA && _prices[1].close < _maData[0]) {
        return false;
    }

    /*
    bool maSignal = false;
    if (!_inpUseMA) {
    // Ignore if we don't care
    maSignal = true;
    }
    else {
    maSignal = _prices[1].close < _maData[0];
    }
    */

    /*
    double avg = (_prices[2].high - _prices[2].low + _prices[3].high - _prices[3].low + _prices[4].high - _prices[4].low) / 3;
    if (currentRange / _inpPinbarRangeThreshhold < avg) {
    return false;
    }
    */

    return true;
}

bool CNewTrend::IsHighestHigh()
{
    for (int bar = 2; bar < _inpBarCountHighestHigh; bar++) {
        if (_prices[1].high <= _prices[bar].high) {
            return false;
        }
    }

    return true;
}

bool CNewTrend::IsLowestLow()
{
    for (int bar = 2; bar < _inpBarCountHighestHigh; bar++) {
        if (_prices[1].low >= _prices[bar].low) {
            return false;
        }
    }

    return true;
}

bool CNewTrend::IsCloseNearHigh()
{
    double range = _prices[1].high - _prices[1].low;
    return ((_prices[1].high - _prices[1].close) <= (range * 0.05));
}

bool CNewTrend::IsCloseNearLow()
{
    double range = _prices[1].high - _prices[1].low;
    return ((_prices[1].close - _prices[1].low) <= (range * 0.05));
}

bool CNewTrend::IsRangeIncreasing()
{
    double currentRange = _prices[1].high - _prices[1].low;
    double prevRange;

    for (int index = 2; index <= 4; index++) {
        prevRange = _prices[index].high - _prices[index].low;
        if (currentRange < prevRange * 2) {
            return false;
        }
    }

    /*prevRange = _prices[2].high - _prices[2].low;
    return (currentRange >= prevRange * 2) && (currentRange < prevRange * 3);*/

    return true;
}

bool CNewTrend::InRange()
{
    // Determine whether we are currently in a range or trend
    // Use the ADX for determining this
    for (int index = 0; index < _inpBarCountInRange; index++) {
        if (_adxData[index] >= _inpADXThreshold) {
            return false;
        }
    }

    return true;
}