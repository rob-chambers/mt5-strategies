#include "CExpertBase.mqh"

class CDerived : public CExpertBase
{
public:
    CDerived(void);
    ~CDerived(void);
    virtual int Init
    (
        double   inpLots = 1,
        double   inpStopLossPips = 30,
        bool     inpUseTakeProfit = true,
        double   inpTakeProfitPips = 40,
        int      inpTrailingStopPips = 30
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
};

CDerived::CDerived(void)
{
}

CDerived::~CDerived(void)
{
}

int CDerived::Init(
    double   inpLots,
    double   inpStopLossPips,
    bool     inpUseTakeProfit,
    double   inpTakeProfitPips,
    int      inpTrailingStopPips
    )
{
    Print("In derived class OnInit");

    return CExpertBase::Init(inpLots, inpStopLossPips, inpUseTakeProfit, inpTakeProfitPips, inpTrailingStopPips);
}

void CDerived::Deinit(void)
{
    Print("In derived class OnDeInit");
    CExpertBase::Deinit();
}

void CDerived::Processing(void)
{
    CExpertBase::Processing();
}