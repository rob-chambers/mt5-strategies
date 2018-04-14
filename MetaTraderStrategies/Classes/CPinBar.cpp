#include "CMyExpertBase.mqh"

class CPinBar : public CMyExpertBase
{
public:
    CPinBar(void);
    ~CPinBar(void);
    virtual int Init
    (
        LOTSIZING_RULE  inpLotSizingRule = Dynamic,
        double          inpDynamicSizingRiskPerTrade = 1,
        double          inpLots = 0,
        STOPLOSS_RULE   inpStopLossRule = CurrentBarNPips,
        int             inpStopLossPips = 3,
        bool            inpUseTakeProfit = false,
        int             inpTakeProfitPips = 0,
        double          inpTakeProfitRiskRewardRatio = 0,
        STOPLOSS_RULE   inpTrailingStopLossRule = None,
        int             inpTrailingStopPips = 20,
        bool            inpMoveToBreakEven = true,
        bool            inpGoLong = true,
        bool            inpGoShort = true,
        bool            inpAlertTerminalEnabled = true,
        bool            inpAlertEmailEnabled = false,
        int             inpMinutesToWaitAfterPositionClosed = 60,
        int             inpMinTradingHour = 0,
        int             inpMaxTradingHour = 0,
        double          inpPinbarThreshhold = 0.67,
        double          inpPinbarRangeThreshhold = 2,
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
    double _inpPinbarThreshhold;
    double _inpPinbarRangeThreshhold;
    bool _inpFilterByMA;
    ENUM_TIMEFRAMES _inpMAPeriodType;
    int _inpMAPeriodAmount;
    int _maHandle;
    double _maData[];
};

CPinBar::CPinBar(void)
{
}

CPinBar::~CPinBar(void)
{
}

int CPinBar::Init(
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
    double          inpPinbarThreshhold,
    double          inpPinbarRangeThreshhold,
    bool            inpFilterByMA,
    ENUM_TIMEFRAMES inpMAPeriodType,
    int             inpMAPeriodAmount
)
{
    Print("In derived class CPinBar OnInit");

    int retCode = CMyExpertBase::Init(inpLotSizingRule, inpDynamicSizingRiskPerTrade, inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit,
        inpTakeProfitPips, inpTakeProfitRiskRewardRatio, inpTrailingStopLossRule, inpTrailingStopPips, inpMoveToBreakEven, inpGoLong, inpGoShort,
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed,
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for pin bar EA");
        
        // Non-base variables initialised here
        _inpPinbarThreshhold = inpPinbarThreshhold;
        _inpPinbarRangeThreshhold = inpPinbarRangeThreshhold;
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

void CPinBar::Deinit(void)
{
    Print("In derived class OnDeInit");
    CMyExpertBase::Deinit();
    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
}

void CPinBar::Processing(void)
{
    CMyExpertBase::Processing();
}

void CPinBar::NewBarAndNoCurrentPositions(void)
{
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
}

bool CPinBar::HasBullishSignal()
{
    /* Rules:
    Current candle low < previous candle low
    Current candle close > previous candle low
    Not a higher high (Current high <= previous high)
    Current candle has a long wick pointing down

    Close must be above moving average
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

    //double avg = (_prices[2].high - _prices[2].low + _prices[3].high - _prices[3].low + _prices[4].high - _prices[4].low) / 3;
    double avg = _atrData[0];
    if (currentRange / _inpPinbarRangeThreshhold < avg) {
        return false;
    }

    if (_inpFilterByMA && _prices[1].close < _maData[0]) {
        return false;
    }

    return true;
}

bool CPinBar::HasBearishSignal()
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

    
    //double avg = (_prices[2].high - _prices[2].low + _prices[3].high - _prices[3].low + _prices[4].high - _prices[4].low) / 3;
    double avg = _atrData[0];
    if (currentRange / _inpPinbarRangeThreshhold < avg) {
        return false;
    }
    
    if (_inpFilterByMA && _prices[1].close > _maData[0]) {
        return false;
    }

    return true;
}