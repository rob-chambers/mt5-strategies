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
        bool     inpAlertEmailEnabled = false
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

    bool IsHighestHigh();
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
    bool     inpAlertEmailEnabled
    )
{
    Print("In derived class CNewTrend OnInit");

    // Non-base variables initialised here
    return CExpertBase::Init(inpLots, inpStopLossPips, inpUseTakeProfit, inpTakeProfitPips, inpTrailingStopPips, inpGoLong, inpGoShort, inpAlertTerminalEnabled, inpAlertEmailEnabled);
}

void CNewTrend::Deinit(void)
{
    Print("In derived class CNewTrend OnDeInit");
    CExpertBase::Deinit();
}

void CNewTrend::Processing(void)
{
    CExpertBase::Processing();
}

void CNewTrend::NewBarAndNoCurrentPositions(void)
{
    int highsDataCount = CopyBuffer(_highsHandle, 0, 0, 40, _highsData);
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

    //double closeFromHigh = _prices[1].high - _prices[1].close;
    //double openFromHigh = _prices[1].high - _prices[1].open;


    /*
    double currentRange = _prices[1].high - _prices[1].low;
    if (!((closeFromHigh / currentRange <= (1 - _inpPinbarThreshhold)) &&
        (openFromHigh / currentRange <= (1 - _inpPinbarThreshhold)))) {
        return false;
    }
    */

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