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
}

bool CJimBrownTrend::HasBullishSignal()
{
    //CheckSignal();
    //bool basicSignal = _sig == "Buy";
    //
    //if (basicSignal) {
    if (GetTrendDirection(1) == "Up") {
        if (_prices[1].close >= _longTermTrendData[1] && _prices[1].low <= _shortTermTrendData[1]) {        
            if (_prices[1].close < _longTermTimeFrameData[1]) {
                Print("Rejecting signal due to long-term trend.");
                return false;
            }

            return true;
        }
    }

    //if (_macdData[1] < 0.001) {

    return false;       
}

bool CJimBrownTrend::HasBearishSignal()
{
    //CheckSignal();
    //bool basicSignal = _sig == "Sell";
    //
    //if (basicSignal) {
    if (GetTrendDirection(1) == "Dn") {
        if (_prices[1].close <= _longTermTrendData[1] && _prices[1].high >= _shortTermTrendData[1]) {
            if (_prices[1].close > _longTermTimeFrameData[1]) {
                Print("Rejecting signal due to long-term trend.");
                return false;
            }

            return true;
        }
    }
    
    return false;
    // && _macdData[1] > 0.001; // Added custom MACD filter
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