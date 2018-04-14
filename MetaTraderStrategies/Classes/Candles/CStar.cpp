#include "CMyExpertBase.mqh"

class CStar : public CMyExpertBase
{
public:
    CStar(void);
    ~CStar(void);
    virtual int Init
    (
        LOTSIZING_RULE  inpLotSizingRule = Dynamic,
        double          inpDynamicSizingRiskPerTrade = 1,
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = StaticPipsValue,
        int             inpStopLossPips = 15,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 30,
        double          inpTakeProfitRiskRewardRatio = 0,
        STOPLOSS_RULE   inpTrailingStopLossRule = None,
        int             inpTrailingStopPips = 0,
        bool            inpMoveToBreakEven = true,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        bool            inpFilterByMA = false,
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

    double AvgBody(int index);
};

CStar::CStar(void)
{
}

CStar::~CStar(void)
{
}

int CStar::Init(
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
    bool            inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int             inpMAPeriodAmount
)
{
    Print("In derived class CStar OnInit");

    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for star EA");
        
        // Non-base variables initialised here
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

void CStar::Deinit(void)
{
    Print("In derived class OnDeInit");
    CMyExpertBase::Deinit();
    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void CStar::Processing(void)
{
    CMyExpertBase::Processing();
}

void CStar::NewBarAndNoCurrentPositions(void)
{
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool CStar::HasBullishSignal()
{
    return false;
}

bool CStar::HasBearishSignal()
{
    double middle = 0.5 * (_prices[3].open + _prices[3].close);
    if ((_prices[3].close - _prices[3].open > AvgBody(1)) &&
        (MathAbs(_prices[2].close - _prices[2].open) < AvgBody(1) * 0.5) &&
        (_prices[2].close > _prices[3].close) &&
        (_prices[2].open > _prices[3].open) &&
        (_prices[1].close < middle) &&
        (_prices[1].high < _prices[2].high) && // custom rule
        (_prices[3].high < _prices[2].high)) // custom rule    

    {
        if (_inpFilterByMA && _prices[1].close > _maData[0]) {
            return false;
        }

        return(true);
    }

    return false;
}

//+------------------------------------------------------------------+
//| Returns the averaged value of candle body size                   |
//+------------------------------------------------------------------+
double CStar::AvgBody(int index)
{
    double candle_body = 0;
    int averagingBarCount = 14;

    ///--- calculate the averaged size of the candle's body
    for (int i = index; i < index + averagingBarCount; i++)
    {
        candle_body += MathAbs(_prices[i].open - _prices[i].close);
    }

    candle_body = candle_body / averagingBarCount;
    return candle_body;
}