#include "CMyExpertBase.mqh"

class CBigMove : public CMyExpertBase
{
public:
    CBigMove(void);
    ~CBigMove(void);
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
        int             inpATRPeriod = 12,
        bool            inpFilterByMA = true,
        ENUM_TIMEFRAMES inpMAPeriodType = PERIOD_D1,
        int             inpMAPeriodAmount = 8,
        int             inpMinMultiplier = 3,
        int             inpMaxMultiplier = 7
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions(void);

private:
    int _atrHandle;
    double _atrData[];
    int _inpATRPeriod;
    bool _inpFilterByMA;
    ENUM_TIMEFRAMES _inpMAPeriodType;
    int _inpMAPeriodAmount;
    int _maHandle;
    double _maData[];
    int _inpMinMultiplier;
    int _inpMaxMultiplier;
};

CBigMove::CBigMove(void)
{
}

CBigMove::~CBigMove(void)
{
}

int CBigMove::Init(
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
    int             inpATRPeriod,
    bool            inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int             inpMAPeriodAmount,
    int             inpMinMultiplier,
    int             inpMaxMultiplier
)
{
    Print("In derived class CBigMove OnInit");

    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for big move EA");

        ArraySetAsSeries(_atrData, true);
        _atrHandle = iATR(Symbol(), PERIOD_CURRENT, inpATRPeriod);   

        _inpATRPeriod = inpATRPeriod;
        _inpFilterByMA = inpFilterByMA;
        _inpMAPeriodType = inpMAPeriodType;
        _inpMAPeriodAmount = inpMAPeriodAmount;
        _inpMinMultiplier = inpMinMultiplier;
        _inpMaxMultiplier = inpMaxMultiplier;

        if (inpFilterByMA) {
            ArraySetAsSeries(_maData, true);
            _maHandle = iMA(_Symbol, inpMAPeriodType, _inpMAPeriodAmount, 0, MODE_EMA, PRICE_CLOSE);
        }
    }

    return retCode;
}

void CBigMove::Deinit(void)
{
    Print("In derived class CBigMove OnDeInit");
    CMyExpertBase::Deinit();
    Print("Releasing ATR indicator handle");
    ReleaseIndicator(_atrHandle);

    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void CBigMove::Processing(void)
{
    CMyExpertBase::Processing();
}

void CBigMove::NewBarAndNoCurrentPositions(void)
{
    int count = CopyBuffer(_atrHandle, 0, 0, _inpATRPeriod, _atrData);
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool CBigMove::HasBullishSignal()
{   
    double currentRange = _prices[1].high - _prices[1].low; 
    if (currentRange >= _atrData[1] * _inpMinMultiplier && currentRange <= _atrData[1] * _inpMaxMultiplier) {

        // What colour candle is it - trade in the opposite direction of the move
        if (_prices[1].close < _prices[1].open) {
            if (_inpFilterByMA && _prices[1].close < _maData[0]) {
                return false;
            }

            return true;
        }
    }

    return false;
}

bool CBigMove::HasBearishSignal()
{
    double currentRange = _prices[1].high - _prices[1].low;
    if (currentRange >= _atrData[1] * _inpMinMultiplier && currentRange <= _atrData[1] * _inpMaxMultiplier) {

        // What colour candle is it - trade in the opposite direction of the move
        if (_prices[1].close > _prices[1].open) {
            if (_inpFilterByMA && _prices[1].close > _maData[0]) {
                return false;
            }

            return true;
        }
    }

    return false;
}