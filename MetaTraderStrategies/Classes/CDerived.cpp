#include "CExpertBase.mqh"

class CDerived : public CExpertBase
{
public:
    CDerived(void);
    ~CDerived(void);
    virtual int Init
    (
        double   inpLots = 1,
        double   inpStopLossPips = 30,
        bool     inpUseTakeProfit = true,
        double   inpTakeProfitPips = 40,
        int      inpTrailingStopPips = 30,
        bool     inpGoLong = true,
        bool     inpGoShort = true,
        double   inpPinbarThreshhold = 0.6,
        double   inpPinbarRangeThreshhold = 1,
        bool     inpAlertTerminalEnabled = true,
        bool     inpAlertEmailEnabled = false
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

private:
    double _inpPinbarThreshhold;
    double _inpPinbarRangeThreshhold;
};

CDerived::CDerived(void)
{
}

CDerived::~CDerived(void)
{
}

int CDerived::Init(
    double   inpLots,
    double   inpStopLossPips,
    bool     inpUseTakeProfit,
    double   inpTakeProfitPips,
    int      inpTrailingStopPips,
    bool     inpGoLong,
    bool     inpGoShort,
    double   inpPinbarThreshhold,
    double   inpPinbarRangeThreshhold,
    bool     inpAlertTerminalEnabled,
    bool     inpAlertEmailEnabled
    )
{
    Print("In derived class OnInit");

    // Non-base variables initialised here
    _inpPinbarThreshhold = inpPinbarThreshhold;
    _inpPinbarRangeThreshhold = inpPinbarRangeThreshhold;

    return CExpertBase::Init(inpLots, inpStopLossPips, inpUseTakeProfit, inpTakeProfitPips, inpTrailingStopPips, inpGoLong, inpGoShort, inpAlertTerminalEnabled, inpAlertEmailEnabled);
}

void CDerived::Deinit(void)
{
    Print("In derived class OnDeInit");
    CExpertBase::Deinit();
}

void CDerived::Processing(void)
{
    CExpertBase::Processing();
}

bool CDerived::HasBullishSignal()
{
    /* Rules:
    Current candle low < previous candle low
    Current candle close > previous candle low
    Not a higher high (Current high <= previous high)
    Current candle has a long wick pointing down

    Close must be above moving average (200 period by default)
    */
    if (!(_prices[1].low < _prices[2].low)) return false;
    if (!(_prices[1].close > _prices[2].low)) return false;
    if (_prices[1].high > _prices[2].high) return false;

    double closeFromHigh = _prices[1].high - _prices[1].close;
    double openFromHigh = _prices[1].high - _prices[1].open;

    double currentRange = _prices[1].high - _prices[1].low;
    if (!((closeFromHigh / currentRange <= (1 - _inpPinbarThreshhold)) &&
        (openFromHigh / currentRange <= (1 - _inpPinbarThreshhold)))) {
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

    double avg = (_prices[2].high - _prices[2].low + _prices[3].high - _prices[3].low + _prices[4].high - _prices[4].low) / 3;
    if (currentRange / _inpPinbarRangeThreshhold < avg) {
        return false;
    }
    
    return true;
}

bool CDerived::HasBearishSignal()
{
    /* Rules:
    Current candle high > previous candle high
    Current candle close < previous candle high
    Not a lower low (Current low >= previous low)
    Current candle has a long wick pointing up

    Close must be below moving average (200 period by default)

    For significant bars, check range of last 3 bars.  Current bar range > 2x

    */
    if (!(_prices[1].high > _prices[2].high)) return false;
    if (!(_prices[1].close < _prices[2].high)) return false;
    if (_prices[1].low < _prices[2].low) return false;

    double closeFromHigh = _prices[1].high - _prices[1].close;
    double openFromHigh = _prices[1].high - _prices[1].open;

    double currentRange = _prices[1].high - _prices[1].low;
    if (!((closeFromHigh / currentRange >= _inpPinbarThreshhold) &&
        (openFromHigh / currentRange >= _inpPinbarThreshhold))) {
        return false;
    }

    /*
    bool maSignal = false;
    if (!_inpUseMA) {
        // Ignore if we don't care
        maSignal = true;
    }
    else {
        maSignal = _prices[1].close > _maData[0];
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