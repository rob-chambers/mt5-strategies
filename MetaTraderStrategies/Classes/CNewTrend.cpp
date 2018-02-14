#include "CExpertBase.mqh"

class CNewTrend : public CExpertBase
{
public:
    CNewTrend(void);
    ~CNewTrend(void);
    virtual int Init
    (
        double   inpLots = 1,
        double   inpStopLossPips = 15,
        bool     inpUseTakeProfit = true,
        double   inpTakeProfitPips = 30,
        int      inpTrailingStopPips = 20,
        bool     inpGoLong = true,
        bool     inpGoShort = true,
        bool     inpAlertTerminalEnabled = true,
        bool     inpAlertEmailEnabled = false,
        int      inpMinutesToWaitAfterPositionClosed = 60,
        int      inpMinTradingHour = 0,
        int      inpMaxTradingHour = 0,
        bool     inpFilterByADX = true,
        int      inpADXPeriod = 14,
        int      inpBarCountInRange = 10,
        int      inpADXThreshold = 30,
        bool     inpFilterByMA = true,
        ENUM_TIMEFRAMES inpMAPeriodType = PERIOD_H1,
        int inpMAPeriodAmount = 21
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
    bool InRange();
};

CNewTrend::CNewTrend(void)
{
}

CNewTrend::~CNewTrend(void)
{
}

int CNewTrend::Init(
    double   inpLots,
    double   inpStopLossPips,
    bool     inpUseTakeProfit,
    double   inpTakeProfitPips,
    int      inpTrailingStopPips,
    bool     inpGoLong,
    bool     inpGoShort,
    bool     inpAlertTerminalEnabled,
    bool     inpAlertEmailEnabled,
    int      inpMinutesToWaitAfterPositionClosed,
    int      inpMinTradingHour,
    int      inpMaxTradingHour,
    bool     inpFilterByADX,
    int      inpADXPeriod,
    int      inpBarCountInRange,
    int      inpADXThreshold,
    bool     inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int      inpMAPeriodAmount
    )
{
    Print("In derived class CNewTrend OnInit");

    // Non-base variables initialised here
    int retCode = CExpertBase::Init(inpLots, inpStopLossPips, inpUseTakeProfit, inpTakeProfitPips, inpTrailingStopPips, inpGoLong, inpGoShort, inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed, inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for new trend EA");
        ArraySetAsSeries(_adxData, true);

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
    CExpertBase::Deinit();
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
    CExpertBase::Processing();
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
    */
    if (!(_prices[1].close > _prices[1].open)) return false;
    if (!(_prices[1].high > _prices[2].high)) return false;

    if (!IsHighestHigh()) return false;

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
    
    */
    return false;

    //return true;
}

bool CNewTrend::IsHighestHigh()
{
    int bars = 40;
    for (int bar = 2; bar < bars; bar++) {
        if (_prices[1].high <= _prices[bar].high) {
            return false;
        }        
    }

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