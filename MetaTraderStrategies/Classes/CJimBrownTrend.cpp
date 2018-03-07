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
        int             inpFTF_WP = 3
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions();
    virtual void OnRecentlyClosedTrade();

private:
    int _platinumHandle;
    int _qqeHandle;
    double _platinumUpCrossData[];
    double _platinumDownCrossData[];
    double _macdData[];
    double _qqe1Data[];
    double _qqe2Data[];
    int _inpFTF_RSI_Period;
    int _inpSmoothPlatinum;
    int _inpSlowPlatinum;
    string _trend;
    string _filter1;
    string _sig;

    string GetTrendDirection(int index);
    void CheckSignal();
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
    int             inpFTF_WP
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
        ArraySetAsSeries(_qqe1Data, true);
        ArraySetAsSeries(_qqe2Data, true);

        _platinumHandle = iCustom(_Symbol, PERIOD_CURRENT, "MACD_Platinum", inpFastPlatinum, inpSlowPlatinum, inpSmoothPlatinum, true, true, false, false);
        _qqeHandle = iCustom(_Symbol, PERIOD_CURRENT, "QQE Adv", inpFTF_SF, inpFTF_RSI_Period, inpFTF_WP);

        _inpSlowPlatinum = inpSlowPlatinum;
        _inpSmoothPlatinum = inpSmoothPlatinum;
        _inpFTF_RSI_Period = inpFTF_RSI_Period;

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
    ReleaseIndicator(_qqeHandle);
}

void CJimBrownTrend::Processing(void)
{
    CMyExpertBase::Processing();
}

void CJimBrownTrend::OnRecentlyClosedTrade()
{
    //Print("Resetting trend status");
    //_trend = "X";
    //_sig = "Start";
}

void CJimBrownTrend::NewBarAndNoCurrentPositions()
{
    int count = CopyBuffer(_qqeHandle, 0, 0, _inpFTF_RSI_Period, _qqe1Data);
    if (count == -1) {
        Print("Error copying qqe1 data.");
        return;
    }

    count = CopyBuffer(_qqeHandle, 1, 0, _inpFTF_RSI_Period, _qqe2Data);
    if (count == -1) {
        Print("Error copying qqe2 data.");
        return;
    }

    count = CopyBuffer(_platinumHandle, 0, 0, _inpSlowPlatinum, _macdData);
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
}

bool CJimBrownTrend::HasBullishSignal()
{
    CheckSignal();
    return _sig == "Buy";
}

bool CJimBrownTrend::HasBearishSignal()
{
    CheckSignal();
    return _sig == "Sell";
}

void CJimBrownTrend::CheckSignal()
{    
    /*
    _filter1 = "Both";

    _filter1 = "";
    for (int i = 1; _filter1 == ""; i++)
    {
        if (GetTrendDirection(i) == "Up") {
            _filter1 = "Buy";
        }
        else if (GetTrendDirection(i) == "Dn") {
            _filter1 = "Sell";
        }
    }
    */

    _filter1 = "Both";

    // Start of proper signal
    if (_trend != "Up" && GetTrendDirection(1) == "Up")
    {
        _trend = "Up";
        _sig = "X";
    }
    else if (_trend != "Dn" && GetTrendDirection(1) == "Dn")
    {
        _trend = "Dn";
        _sig = "X";
    }

    if (_sig == "X")
    {
        if (_trend == "Up" && (_filter1 == "Both" || _filter1 == "Buy"))
        {
            _sig = "Buy";
        }
        else if (_trend == "Dn" && (_filter1 == "Both" || _filter1 == "Sell"))
        {
            _sig = "Sell";
        }
    }
}

string CJimBrownTrend::GetTrendDirection(int index)
{
    string trend = "X";
       
    double blue = _platinumUpCrossData[index];
    double orange = _platinumDownCrossData[index];
    double qqe1 = _qqe1Data[index];
    double qqe2 = _qqe2Data[index];
    
    if (blue > -1 && blue < 1 && qqe1 > qqe2) {
        trend = "Up";
    }
    else if (orange > -1 && orange < 1 && qqe1 < qqe2) {
        trend = "Dn";
    }

    return trend;
}