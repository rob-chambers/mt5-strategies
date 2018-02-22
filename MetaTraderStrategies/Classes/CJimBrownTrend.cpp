#include "CMyExpertBase.mqh"

class CJimBrownTrend : public CMyExpertBase
{
public:
    CJimBrownTrend(void);
    ~CJimBrownTrend(void);
    virtual int Init
    (
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = StaticPipsValue,
        int             inpStopLossPips = 15,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 30,
        STOPLOSS_RULE   inpTrailingStopLossRule = StaticPipsValue,
        int             inpTrailingStopPips = 20,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        int             inpMAPeriod = 12
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions();
    virtual void OnRecentlyClosedTrade();

private:
    int _inpMAPeriod;
    int _qmpHandle;
    double _qmpData[];
    double _qmpDownData[];
    string _trend;

    string GetTrendDirection(ENUM_TIMEFRAMES timeFrame, int index);
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
    bool            inpGoLong,
    bool            inpGoShort,
    bool            inpAlertTerminalEnabled,
    bool            inpAlertEmailEnabled,
    int             inpMinutesToWaitAfterPositionClosed,
    int             inpMinTradingHour,
    int             inpMaxTradingHour,
    int             inpMAPeriod
    )
{
    Print("In derived class CJimBrownTrend OnInit");

    // Non-base variables initialised here
    int retCode = CMyExpertBase::Init(inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit, 
        inpTakeProfitPips, inpTrailingStopLossRule, inpTrailingStopPips, inpGoLong, inpGoShort, 
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed, 
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for Jim Brown's trend following EA");

        _inpMAPeriod = inpMAPeriod;

        ArraySetAsSeries(_qmpData, true);
        ArraySetAsSeries(_qmpDownData, true);

        _qmpHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", 0, 12, 26, 9, true, 1, 8, 3, false, false);
        _trend = "X";
    }

    return retCode;
}

void CJimBrownTrend::Deinit(void)
{
    Print("In derived class CJimBrownTrend OnDeInit");
    CMyExpertBase::Deinit();

    Print("Releasing QMP indicator handle");
    ReleaseIndicator(_qmpHandle);
}

void CJimBrownTrend::Processing(void)
{
    CMyExpertBase::Processing();
}

void CJimBrownTrend::OnRecentlyClosedTrade()
{
    Print("Resetting trend status");
    _trend = "X";
}

void CJimBrownTrend::NewBarAndNoCurrentPositions()
{
    int count = CopyBuffer(_qmpHandle, 0, 0, _inpMAPeriod, _qmpData);
    count = CopyBuffer(_qmpHandle, 1, 0, _inpMAPeriod, _qmpDownData);    
}

bool CJimBrownTrend::HasBullishSignal()
{    
    //Print("Current trend is: ", _trend);

    /*for (int i = 1; i < _inpMAPeriod; i++) {
        if (trend != "Up" && GetTrendDirection(PERIOD_CURRENT, i) == "Up")
        {
            trend = "Up";
            Print("Found up trend");
            return true;
        }
        else if (trend != "Dn" && GetTrendDirection(PERIOD_CURRENT, i) == "Dn")
        {
            Print("Found down trend");
            trend = "Dn";
        }
    }*/

    string oldTrend = _trend;

    if (_trend != "Up" && GetTrendDirection(PERIOD_CURRENT, 1) == "Up")
    {
        _trend = "Up";
        Print("Found up trend");
        return oldTrend == "Dn";
    } else if (_trend != "Dn" && GetTrendDirection(PERIOD_CURRENT, 1) == "Dn")
    {
        Print("Found down trend");
        _trend = "Dn";
    }    

    //if (_qmpData[1] > 0) {
    //    /*for (int i = 0; i < _inpMAPeriod; i++) {
    //        printf("QMP Data %d = %f", i, _qmpData[i]);
    //    }
    //    for (int i = 0; i < _inpMAPeriod; i++) {
    //        printf("QMP Down Data %d = %f", i, _qmpDownData[i]);
    //    }*/

    //    return true;
    //}

    return false;
}

bool CJimBrownTrend::HasBearishSignal()
{
    return false;
}

string CJimBrownTrend::GetTrendDirection(ENUM_TIMEFRAMES timeFrame, int index)
{
    string trend = "X";
    
    //double blue = _macd1Data[index];
    //double orange = _macd2Data[index];
    //double qqe1 = _qqeAdv1Data[index];
    //double qqe2 = _qqeAdv2Data[index];

    ////printf("CI: %d, %f, %f, %f, %f", index, blue, orange, qqe1, qqe2);

    //if (blue >= orange && qqe1 >= qqe2) {
    //    trend = "Up";
    //}
    //else if (blue < orange && qqe1 < qqe2) {
    //    trend = "Dn";
    //}

    //return trend;
    if (_qmpData[index] > 0) {
        trend = "Up";
    }
    else if (_qmpDownData[index] > 0) {
        trend = "Dn";
    }

    return trend;
}