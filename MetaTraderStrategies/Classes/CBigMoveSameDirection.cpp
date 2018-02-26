#include "CMyExpertBase.mqh"

class CBigMoveSameDirection : public CMyExpertBase
{
public:
    CBigMoveSameDirection(void);
    ~CBigMoveSameDirection(void);
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
    double _rangeData[];
    int _inpATRPeriod;
    bool _inpFilterByMA;
    ENUM_TIMEFRAMES _inpMAPeriodType;
    int _inpMAPeriodAmount;
    int _maHandle;
    double _maData[];
    int _inpMinMultiplier;
    int _inpMaxMultiplier;
};

CBigMoveSameDirection::CBigMoveSameDirection(void)
{
}

CBigMoveSameDirection::~CBigMoveSameDirection(void)
{
}

int CBigMoveSameDirection::Init(
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
int             inpATRPeriod,
bool            inpFilterByMA,
ENUM_TIMEFRAMES inpMAPeriodType,
int             inpMAPeriodAmount,
int             inpMinMultiplier,
int             inpMaxMultiplier
)
{
Print("In derived class CBigMoveSameDirection OnInit");

int retCode = CMyExpertBase::Init(inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
    inpTakeProfitPips, inpTrailingStopLossRule, inpTrailingStopPips, inpGoLong, inpGoShort,
    inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
    inpMinTradingHour, inpMaxTradingHour);

if (retCode == INIT_SUCCEEDED) {
    Print("Custom initialisation for big move EA");

    ArraySetAsSeries(_rangeData, true);
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

void CBigMoveSameDirection::Deinit(void)
{
    Print("In derived class CBigMoveSameDirection OnDeInit");
    CMyExpertBase::Deinit();
    Print("Releasing ATR indicator handle");
    ReleaseIndicator(_atrHandle);

    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void CBigMoveSameDirection::Processing(void)
{
    CMyExpertBase::Processing();
}

void CBigMoveSameDirection::NewBarAndNoCurrentPositions(void)
{
    int count = CopyBuffer(_atrHandle, 0, 0, _inpATRPeriod, _rangeData);
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool CBigMoveSameDirection::HasBullishSignal()
{
    double currentRange = _prices[1].high - _prices[1].low;
    if (currentRange >= _rangeData[1] * _inpMinMultiplier && currentRange <= _rangeData[1] * _inpMaxMultiplier) {

        // What colour candle is it - trade in the SAME direction as the move
        if (_prices[1].close > _prices[1].open) {
            if (_inpFilterByMA && _prices[1].close < _maData[0]) {
                return false;
            }

            if ((_prices[1].close - _prices[1].low) / currentRange <= 0.05 &&
                (_prices[1].high - _prices[1].open) / currentRange <= 0.05) {
                return true;
            }
        }
    }

    return false;
}

bool CBigMoveSameDirection::HasBearishSignal()
{
    double currentRange = _prices[1].high - _prices[1].low;
    if (currentRange >= _rangeData[1] * _inpMinMultiplier && currentRange <= _rangeData[1] * _inpMaxMultiplier) {

        // What colour candle is it - trade in the SAME direction as the move
        if (_prices[1].close < _prices[1].open) {
            if (_inpFilterByMA && _prices[1].close > _maData[0]) {
                return false;
            }

            if ((_prices[1].close - _prices[1].low) / currentRange <= 0.05 &&
                (_prices[1].high - _prices[1].open) / currentRange <= 0.05) {
                return true;
            }
        }
    }

    return false;
}