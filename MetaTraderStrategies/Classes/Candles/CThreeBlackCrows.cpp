#include "CMyExpertBase.mqh"

class CThreeBlackCrows : public CMyExpertBase
{
public:
    CThreeBlackCrows(void);
    ~CThreeBlackCrows(void);
    virtual int Init
    (
        double          inpLots = 1,
        STOPLOSS_RULE   inpStopLossRule = StaticPipsValue,
        int             inpStopLossPips = 15,
        bool            inpUseTakeProfit = true,
        int             inpTakeProfitPips = 30,
        STOPLOSS_RULE   inpTrailingStopLossRule = StaticPipsValue,
        int             inpTrailingStopPips = 20,
        bool            inpGoLong = false,
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
    double AverageBody(int index);
    double MidPoint(int index);
    virtual void NewBarAndNoCurrentPositions(void);

private:
    int _inpMAPeriod;
    /*
    int _maHandle;
    double _maData[];
    */
};

CThreeBlackCrows::CThreeBlackCrows(void)
{
}

CThreeBlackCrows::~CThreeBlackCrows(void)
{
}

int CThreeBlackCrows::Init(
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
    Print("In derived class CThreeBlackCrows OnInit");

    // Non-base variables initialised here
    int retCode = CMyExpertBase::Init(inpLots, inpStopLossRule, inpStopLossPips, inpUseTakeProfit, 
        inpTakeProfitPips, inpTrailingStopLossRule, inpTrailingStopPips, inpGoLong, inpGoShort, 
        inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed, 
        inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for three black crows EA");
        //ArraySetAsSeries(_maData, true);
        //_maHandle = iMA(_Symbol, _inpMAPeriodType, _inpMAPeriodAmount, 0, MODE_EMA, PRICE_CLOSE);
        _inpMAPeriod = inpMAPeriod;
    }

    return retCode;
}

void CThreeBlackCrows::Deinit(void)
{
    Print("In derived class CThreeBlackCrows OnDeInit");
    CMyExpertBase::Deinit();

    /*
    if (_inpFilterByMA) {
        Print("Releasing MA indicator handle");
        ReleaseIndicator(_maHandle);
    }
    */
}

void CThreeBlackCrows::Processing(void)
{
    CMyExpertBase::Processing();
}

void CThreeBlackCrows::NewBarAndNoCurrentPositions(void)
{
    /*
    if (_inpFilterByMA) {
        int maDataCount = CopyBuffer(_maHandle, 0, 0, _inpMAPeriodAmount, _maData);
    }
    */
}

bool CThreeBlackCrows::HasBullishSignal()
{
    return false;
}

//+------------------------------------------------------------------+
//| Returns the averaged value of candle body size                   |
//+------------------------------------------------------------------+
double CThreeBlackCrows::AverageBody(int index)
{
    double candle_body = 0;
    for (int i = index; i < index + _inpMAPeriod; i++)
    {
        candle_body += MathAbs(_prices[i].open - _prices[i].close);
    }

    candle_body = candle_body / _inpMAPeriod;
    return candle_body;
}

double CThreeBlackCrows::MidPoint(int index)
{
    return 0.5 * (_prices[index].high + _prices[index].low);
}

bool CThreeBlackCrows::HasBearishSignal()
{
    if (!((_prices[3].open - _prices[3].close) > AverageBody(1))) return false;
    if (!((_prices[2].open - _prices[2].close) > AverageBody(1))) return false;
    if (!((_prices[1].open - _prices[1].close) > AverageBody(1))) return false;

    if (!(MidPoint(2) < MidPoint(3))) return false;
    if (!(MidPoint(1) < MidPoint(2))) return false;

    return true;
}