#include "CExpertBase.mqh"

class CDerived : CExpertBase
{
public:
    CDerived(void);
    ~CDerived(void);
    int               Init(void);
    void              Deinit(void);
    void              Processing(void);
};

CDerived::CDerived(void)
{
}

CDerived::~CDerived(void)
{
}

int CDerived::Init(void)
{
    Print("In derived class OnInit");
    return 0;
}

void CDerived::Deinit(void)
{
    Print("In derived class OnDeInit");
}

void CDerived::Processing(void)
{
    Print("In derived processing");
}