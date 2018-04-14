#include "CMyExpertBase.mqh"

class COutsideBar : public CMyExpertBase
{
public:
    COutsideBar(void);
    ~COutsideBar(void);
    virtual int Init
    (
        LOTSIZING_RULE  inpLotSizingRule = Dynamic,
        double          inpDynamicSizingRiskPerTrade = 1,
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = CurrentBarNPips,
        int             inpStopLossPips = 0,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 60,
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
        bool            inpFilterByMA = true,
        ENUM_TIMEFRAMES inpMAPeriodType = PERIOD_D1,
        int             inpMAPeriodAmount = 8
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions(void);

private:
    bool _inpFilterByMA;
    ENUM_TIMEFRAMES _inpMAPeriodType;
    int _inpMAPeriodAmount;
    int _maHandle;
    double _maData[];
};

COutsideBar::COutsideBar(void)
{
}

COutsideBar::~COutsideBar(void)
{
}

int COutsideBar::Init(
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
    bool     inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int      inpMAPeriodAmount
    )
{
    Print("In derived class COutsideBar OnInit");

    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for outside bar EA");
        _inpFilterByMA = inpFilterByMA;
        _inpMAPeriodType = inpMAPeriodType;
        _inpMAPeriodAmount = inpMAPeriodAmount;

        if (inpFilterByMA) {
            ArraySetAsSeries(_maData, true);
            _maHandle = iMA(_Symbol, inpMAPeriodType, _inpMAPeriodAmount, 0, MODE_EMA, PRICE_CLOSE);
        }
    }

    return retCode;
}

void COutsideBar::Deinit(void)
{
    Print("In derived class COutsideBar OnDeInit");
    CMyExpertBase::Deinit();

    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void COutsideBar::Processing(void)
{
    CMyExpertBase::Processing();
}

void COutsideBar::NewBarAndNoCurrentPositions(void)
{
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool COutsideBar::HasBullishSignal()
{
    /* Rules:
    1) Open is lower than the previous close
    2) Close is higher than the previous open
    3) Previous bar's close is lower than it's open
    4) Current open is lower then previous bar's low
    5) Current close is higher than previous bar's high
    */
    if (!(_prices[1].open < _prices[2].close)) return false;
    if (!(_prices[1].close > _prices[2].open)) return false;
    if (!(_prices[2].close < _prices[2].open)) return false;
    if (!(_prices[1].open < _prices[2].low)) return false;
    if (!(_prices[1].close > _prices[2].high)) return false;

    if (_inpFilterByMA && _prices[1].close < _maData[0]) {
        Print("Avoiding as close is lower than MA");
        return false;
    }

    return true;
}

bool COutsideBar::HasBearishSignal()
{
    /* Rules:
    1) Open is higher than the previous close
    2) Close is lower than the previous open
    3) Previous bar's close is higher than it's open
    4) Current open is higher then previous bar's high
    5) Current close is lower than previous bar's low
    */
    if (!(_prices[1].high > _prices[2].close)) return false;
    if (!(_prices[1].close < _prices[2].open)) return false;
    if (!(_prices[2].close > _prices[2].open)) return false;
    if (!(_prices[1].open > _prices[2].high)) return false;
    if (!(_prices[1].close < _prices[2].low)) return false;

    if (_inpFilterByMA && _prices[1].close > _maData[0]) {
        Print("Avoiding as close is higher than MA");
        return false;
    }

    return true;
}