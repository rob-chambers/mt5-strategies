#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh> 
#include <Trade\PositionInfo.mqh>

class CExpertBase
{
public:
    CExpertBase(void);
    ~CExpertBase(void);

    int               Init(void);
    void              Deinit(void);
    void              Processing(void);
};

CExpertBase::CExpertBase(void)
{
}

CExpertBase::~CExpertBase(void)
{
}

int CExpertBase::Init(void)
{
    Print("In base class OnInit");
    return 0;
}

void CExpertBase::Deinit(void)
{
    Print("In base class OnDeInit");
}

void CExpertBase::Processing(void)
{
    Print("In base processing");
    //--- we work only at the time of the birth of new bar
    /*static datetime PrevBars = 0;
    datetime time_0 = iTime(0);
    if (time_0 == PrevBars)
        return;

    PrevBars = time_0;*/
}

////+------------------------------------------------------------------+ 
////| Get Time for specified bar index                                 | 
////+------------------------------------------------------------------+ 
//datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT)
//{
//    if (symbol == NULL)
//        symbol = _symbol.Name();
//    if (timeframe == 0)
//        timeframe = Period();
//    datetime Time[1];
//    datetime time = 0;
//    int copied = CopyTime(symbol, timeframe, index, 1, Time);
//    if (copied > 0) time = Time[0];
//    return(time);
//}