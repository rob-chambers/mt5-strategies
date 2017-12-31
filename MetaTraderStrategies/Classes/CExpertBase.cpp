#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh> 
#include <Trade\PositionInfo.mqh>

class CExpertBase
{
public:
    CExpertBase(void);
    ~CExpertBase(void);

    virtual int Init
    (
        double   inpLots,
        double   inpStopLossPips,
        bool     inpUseTakeProfit,
        double   inpTakeProfitPips,
        int      inpTrailingStopPips
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);

protected:
    CSymbolInfo    _symbol;                     // symbol info object
    MqlRates _prices[];
    int _digits_adjust;
    double _adjustedPoints;
    double _trailing_stop;
    double _currentBid, _currentAsk;
    double   _inpLots;
    double   _inpStopLossPips;
    bool     _inpUseTakeProfit;
    double   _inpTakeProfitPips;
    int      _inpTrailingStopPips;

private:
    bool RefreshRates();
    datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT);
    bool CheckToModifyPositions();
};

CExpertBase::CExpertBase(void)
{
}

CExpertBase::~CExpertBase(void)
{
}

int CExpertBase::Init(
    double   lots,
    double   stopLossPips,
    bool     useTakeProfit,
    double   takeProfitPips,
    int      trailingStopPips
)
{
    Print("In base class OnInit");
    if (!_symbol.Name(Symbol())) // sets symbol name
        return(INIT_FAILED);

    if (!RefreshRates()) {
        Print("Could not refresh rates - init failed.");
        return(INIT_FAILED);
    }

    ArraySetAsSeries(_prices, true);
    //ArraySetAsSeries(_maData, true);
    //_maHandle = iMA(Symbol(), _inpMovingAveragePeriodType, _inpMovingAveragePeriodAmount, 0, MODE_SMA, PRICE_CLOSE);

    _digits_adjust = 1;
    if (_Digits == 5 || _Digits == 3 || _Digits == 1) {
        _digits_adjust = 10;
    }

    _adjustedPoints = _symbol.Point() * _digits_adjust;

    _inpLots = lots;
    _inpStopLossPips = stopLossPips;
    _inpUseTakeProfit = useTakeProfit;
    _inpTakeProfitPips = takeProfitPips;
    _inpTrailingStopPips = trailingStopPips;

    _trailing_stop = trailingStopPips * _adjustedPoints;

    printf("DA=%f, adjusted points = %f", _digits_adjust, _adjustedPoints);

    return(INIT_SUCCEEDED);
}

void CExpertBase::Deinit(void)
{
    Print("In base class OnDeInit");
}

void CExpertBase::Processing(void)
{
    //--- we work only at the time of the birth of new bar
    static datetime PrevBars = 0;
    datetime time_0 = iTime(0);
    if (time_0 == PrevBars) return;

    Print("New bar found");
    PrevBars = time_0;

    double stopLossPipsFinal;
    double takeProfitPipsFinal;
    double stopLossLevel;
    double takeProfitLevel;
    double stopLevelPips;

    // -------------------- Collect most current data --------------------
    if (!RefreshRates()) {
        return;
    }

    int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 10, _prices); // Collects data from shift 0 to shift 9

    // -------------------- EXITS --------------------

    if (PositionSelect(_Symbol) == true) // We have an open position
    {
        CheckToModifyPositions();
        return;
    }

    // -------------------- ENTRIES --------------------  
    if (PositionSelect(_Symbol) == false) // We have no open positions
    {
        numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 10, _prices); // Collects data from shift 0 to shift 9

        stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _digits_adjust; // Defining minimum StopLevel
        if (_inpStopLossPips < stopLevelPips) {
            stopLossPipsFinal = stopLevelPips;
        }
        else {
            stopLossPipsFinal = _inpStopLossPips;
        }

        if (_inpTakeProfitPips < stopLevelPips) {
            takeProfitPipsFinal = stopLevelPips;
        }
        else {
            takeProfitPipsFinal = _inpTakeProfitPips;
        }

        double limitPrice;

        /*
        if (_inpGoLong && HasBullishSignal()) {
            limitPrice = _currentAsk;
            stopLossLevel = limitPrice - stopLossPipsFinal * _Point * _digits_adjust;
            if (_inpUseTakeProfit) {
                takeProfitLevel = limitPrice + takeProfitPipsFinal * _Point * _digits_adjust;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_BUY, _inpLots, limitPrice, stopLossLevel, takeProfitLevel);
        }
        else if (_inpGoShort && HasBearishSignal()) {
            limitPrice = _currentBid;

            stopLossLevel = limitPrice + stopLossPipsFinal * _Point * _digits_adjust;
            if (_inpUseTakeProfit) {
                takeProfitLevel = limitPrice - takeProfitPipsFinal * _Point * _digits_adjust;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_SELL, _inpLots, limitPrice, stopLossLevel, takeProfitLevel);
        }

        */
    }
}

////+------------------------------------------------------------------+ 
////| Get Time for specified bar index                                 | 
////+------------------------------------------------------------------+ 
datetime CExpertBase::iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT)
{
    if (symbol == NULL)
        symbol = _symbol.Name();
    if (timeframe == 0)
        timeframe = Period();
    datetime Time[1];
    datetime time = 0;
    int copied = CopyTime(symbol, timeframe, index, 1, Time);
    if (copied > 0) time = Time[0];
    return(time);
}

//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool CExpertBase::RefreshRates()
{
    //--- refresh rates
    if (!_symbol.RefreshRates())
        return(false);
    //--- protection against the return value of "zero"
    if (_symbol.Ask() == 0 || _symbol.Bid() == 0)
        return(false);
    //---

    _currentBid = _symbol.Bid();
    _currentAsk = _symbol.Ask();

    return(true);
}

bool CExpertBase::CheckToModifyPositions()
{
    return false;
}