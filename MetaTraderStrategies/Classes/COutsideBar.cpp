#include "CExpertBase.mqh"

class COutsideBar : public CExpertBase
{
public:
    COutsideBar(void);
    ~COutsideBar(void);
    virtual int Init
    (
        double   inpLots = 1,
        bool     inpUseDynamicStops = false,
        double   inpStopLossPips = 15,
        bool     inpUseTakeProfit = true,
        double   inpTakeProfitPips = 30,
        int      inpTrailingStopPips = 20,
        bool     inpGoLong = true,
        bool     inpGoShort = true,
        bool     inpAlertTerminalEnabled = true,
        bool     inpAlertEmailEnabled = false,
        int      inpMinutesToWaitAfterPositionClosed = 60,
        int      inpMinTradingHour = 0,
        int      inpMaxTradingHour = 0
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    virtual void NewBarAndNoCurrentPositions(void);
};

COutsideBar::COutsideBar(void)
{
}

COutsideBar::~COutsideBar(void)
{
}

int COutsideBar::Init(
    double   inpLots,
    bool     inpUseDynamicStops,
    double   inpStopLossPips,
    bool     inpUseTakeProfit,
    double   inpTakeProfitPips,
    int      inpTrailingStopPips,
    bool     inpGoLong,
    bool     inpGoShort,
    bool     inpAlertTerminalEnabled,
    bool     inpAlertEmailEnabled,
    int      inpMinutesToWaitAfterPositionClosed,
    int      inpMinTradingHour,
    int      inpMaxTradingHour
    )
{
    Print("In derived class COutsideBar OnInit");

    // Non-base variables initialised here
    int retCode = CExpertBase::Init(inpLots, inpUseDynamicStops, inpStopLossPips, inpUseTakeProfit, inpTakeProfitPips, inpTrailingStopPips, inpGoLong, inpGoShort, inpAlertTerminalEnabled, inpAlertEmailEnabled, inpMinutesToWaitAfterPositionClosed, inpMinTradingHour, inpMaxTradingHour);

    if (retCode == INIT_SUCCEEDED) {
        Print("Custom initialisation for outside bar EA");
    }

    return retCode;
}

void COutsideBar::Deinit(void)
{
    Print("In derived class COutsideBar OnDeInit");
    CExpertBase::Deinit();
}

void COutsideBar::Processing(void)
{
    CExpertBase::Processing();
}

void COutsideBar::NewBarAndNoCurrentPositions(void)
{
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

    return true;
}