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
        inpTakeProfitPips, inpTrailingStopLossRule, inpTrailingStopPips, inpGoLong, inpGoShort, 
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
            return true;
        }
        else if (_trend == "Dn" && (_filter1 == "Both" || _filter1 == "Sell"))
        {
            _sig = "Sell";
        }
    }
    
    /*


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
    */

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

string CJimBrownTrend::GetTrendDirection(int index)
{
    string trend = "X";
       
    double blue = _platinumUpCrossData[index];
    double orange = _platinumDownCrossData[index];
    double qqe1 = _qqe1Data[index];
    double qqe2 = _qqe2Data[index];
    
    string message;
    string data;

    if (blue > -1 && blue < 1 && qqe1 > qqe2) {
        printf("trend dir up for %d: %f, %f, %f, %f", index, blue, orange, qqe1, qqe2);
        
        data = "";
        //for (int i = 0; i < _inpSlowPlatinum; i++) {
        //    printf(_platinumUpCrossData[i]);
        //}
        for (int i = 0; i < _inpSlowPlatinum; i++) {
            StringConcatenate(data, _platinumUpCrossData[i], ",");
        }
        message = "";
        StringConcatenate(message, "Platinum Buy: ", data);
        Print(message);

        data = "";
        for (int i = 0; i < _inpSlowPlatinum; i++) {
            StringConcatenate(data, _platinumDownCrossData[i], ",");
        }
        message = "";
        StringConcatenate(message, "Platinum Sell: ", data);
        Print(message);

        data = "";
        for (int i = 0; i < _inpFTF_RSI_Period; i++) {
            StringConcatenate(data, _qqe1Data[i], ",");
        }
        StringConcatenate(message, "QQE 1: ", data);
        Print(message);

        data = "";
        for (int i = 0; i < _inpFTF_RSI_Period; i++) {
            StringConcatenate(data, _qqe2Data[i], ",");
        }
        StringConcatenate(message, "QQE 2: ", data);
        Print(message);

        trend = "Up";
    }
    else if (orange > -1 && orange < 1 && qqe1 < qqe2) {
        printf("trend dir down for index %d: %f, %f, %f, %f", index, blue, orange, qqe1, qqe2);
        trend = "Dn";
    }

    return trend;
}

/*
string trend(int x)
{
    //----
    string ctrend = "X";
    double blue = iCustom(Symbol(), 0, "MACD_Platinum", Fast_Platinum, Slow_Platinum, Smooth_Platinum, true, false, false, "", false, false, false, 4, x);
    double orange = iCustom(Symbol(), 0, "MACD_Platinum", Fast_Platinum, Slow_Platinum, Smooth_Platinum, true, false, false, "", false, false, false, 5, x);
    double qqe1 = iCustom(Symbol(), 0, "QQE Adv", SF, RSI_Period, WP, 0, x);
    double qqe2 = iCustom(Symbol(), 0, "QQE Adv", SF, RSI_Period, WP, 1, x);
    //----
    if (blue >= orange && qqe1 >= qqe2) ctrend = "Up";
    else if (blue<orange && qqe1<qqe2) ctrend = "Dn";
    //----
    return (ctrend);
}
*/