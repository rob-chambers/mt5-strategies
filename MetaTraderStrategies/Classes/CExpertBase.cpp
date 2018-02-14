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
        int      inpTrailingStopPips,
        bool     inpGoLong,
        bool     inpGoShort,
        bool     inpAlertTerminalEnabled,
        bool     inpAlertEmailEnabled,
        int      inpMinutesToWaitAfterPositionClosed,
        int      inpMinTradingHour,
        int      inpMaxTradingHour
    );
    virtual void              Deinit(void);
    virtual void              Processing(void);
    virtual bool              HasBullishSignal();
    virtual bool              HasBearishSignal();

protected:
    CSymbolInfo _symbol;
    CPositionInfo _position;
    CTrade _trade;
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
    bool     _inpGoLong;
    bool     _inpGoShort;
    bool     _inpAlertTerminalEnabled;
    bool     _inpAlertEmailEnabled;
    int      _inpMinutesToWaitAfterPositionClosed;
    int      _inpMinTradingHour;
    int      _inpMaxTradingHour;

    void ReleaseIndicator(int& handle);
    virtual void NewBarAndNoCurrentPositions();
    virtual bool RecentlyClosedTrade();

private:
    bool RefreshRates();
    datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT);
    bool CheckToModifyPositions();
    void OpenPosition(string symbol, ENUM_ORDER_TYPE orderType, double volume, double price, double stopLoss, double takeProfit);
    bool LongModified();
    bool ShortModified();
    bool IsOutsideTradingHours();

    double _recentHigh;
    double _recentLow;
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
    int      trailingStopPips,
    bool     inpGoLong,
    bool     inpGoShort,
    bool     inpAlertTerminalEnabled,
    bool     inpAlertEmailEnabled,
    int      inpMinutesToWaitAfterPositionClosed,
    int      inpMinTradingHour,
    int      inpMaxTradingHour
)
{
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
    _inpGoLong = inpGoLong;
    _inpGoShort = inpGoShort;

    _trailing_stop = trailingStopPips * _adjustedPoints;

    _inpAlertTerminalEnabled = inpAlertTerminalEnabled;
    _inpAlertEmailEnabled = inpAlertEmailEnabled;
    _inpMinutesToWaitAfterPositionClosed = inpMinutesToWaitAfterPositionClosed;
    _inpMinTradingHour = inpMinTradingHour;
    _inpMaxTradingHour = inpMaxTradingHour;

    printf("DA=%f, adjusted points = %f", _digits_adjust, _adjustedPoints);

    return(INIT_SUCCEEDED);
}

void CExpertBase::Deinit(void)
{
    Print("In base class OnDeInit");
}

void CExpertBase::ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}

void CExpertBase::Processing(void)
{
    //--- we work only at the time of the birth of new bar
    static datetime PrevBars = 0;

    datetime time_0 = iTime(0);
    if (time_0 == PrevBars) return;

    PrevBars = time_0;

    if (IsOutsideTradingHours()) {
        return;
    }

    double stopLossPipsFinal;
    double takeProfitPipsFinal;
    double stopLossLevel;
    double takeProfitLevel;
    double stopLevelPips;

    // -------------------- Collect most current data --------------------
    if (!RefreshRates()) {
        return;
    }

    int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 40, _prices);

    // -------------------- EXITS --------------------

    if (PositionSelect(_Symbol) == true) // We have an open position
    {
        CheckToModifyPositions();
        return;
    }

    // -------------------- ENTRIES --------------------  
    if (PositionSelect(_Symbol) == false) // We have no open positions
    {
        if (RecentlyClosedTrade()) {
            return;
        }

        _recentHigh = 0;
        _recentLow = 999999;

        numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 40, _prices);

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

        NewBarAndNoCurrentPositions();

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
    }
}

void CExpertBase::NewBarAndNoCurrentPositions()
{
    Print("In base class NewBarAndNoCurrentPositions");
}

bool CExpertBase::RecentlyClosedTrade()
{
    datetime to = TimeCurrent();
    datetime from = to - 60 * _inpMinutesToWaitAfterPositionClosed;

    if (!HistorySelect(from, to)) {
        Print("Failed to retrieve order history");
        return false;
    }

    uint orderCount = HistoryOrdersTotal();
    if (orderCount <= 0) return false;

    ulong ticket;
    //--- return order ticket by its position in the list 
    if ((ticket = HistoryOrderGetTicket(orderCount - 1)) > 0) {
        if (HistoryOrderGetString(ticket, ORDER_SYMBOL) == _symbol.Name()) {
            if (HistoryOrderGetInteger(ticket, ORDER_TYPE) == ORDER_TYPE_SELL) {
                // Print("We had a recent sell order so we'll wait a bit");
                return true;
            }
        }
    }

    return false;
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
    if (_inpTrailingStopPips == 0) return false;

    if (!_position.Select(Symbol())) {
        return false;
    }

    if (_position.PositionType() == POSITION_TYPE_BUY) {
        //--- try to close or modify long position
        /*if (LongClosed())
        return(true);*/
        if (LongModified())
            return true;
    }
    else {
        //--- try to close or modify short position
        /*if (ShortClosed())
        return(true);*/
        if (ShortModified())
            return true;
    }

    return false;
}

bool CExpertBase::LongModified()
{
    if (_inpTrailingStopPips <= 0) return false;

    bool res = false;
    if (_prices[1].high > _prices[2].high && _prices[1].high > _recentHigh) {
        _recentHigh = _prices[1].high;
        double sl = NormalizeDouble(_recentHigh - _trailing_stop, _symbol.Digits());
        double tp = _position.TakeProfit();
        if (_position.StopLoss() < sl || _position.StopLoss() == 0.0) {
            //--- modify position
            if (_trade.PositionModify(Symbol(), sl, tp)) {
                printf("Long position by %s to be modified", Symbol());
            }
            else {
                printf("Error modifying position by %s : '%s'", Symbol(), _trade.ResultComment());
                printf("Modify parameters : SL=%f,TP=%f", sl, tp);
            }

            //--- modified and must exit from expert
            res = true;
        }
    }

    return res;
}

bool CExpertBase::ShortModified()
{
    if (_inpTrailingStopPips <= 0) return false;

    bool res = false;
    if (_prices[1].low < _prices[2].low && _prices[1].low < _recentLow) {
        _recentLow = _prices[1].low;

        double sl = NormalizeDouble(_recentLow + _trailing_stop, _symbol.Digits());
        double tp = _position.TakeProfit();
        if (_position.StopLoss() > sl || _position.StopLoss() == 0.0) {
            //--- modify position
            if (_trade.PositionModify(Symbol(), sl, tp)) {
                printf("Short position by %s to be modified", Symbol());
            }
            else {
                printf("Error modifying position by %s : '%s'", Symbol(), _trade.ResultComment());
                printf("Modify parameters : SL=%f,TP=%f", sl, tp);
            }

            //--- modified and must exit from expert
            res = true;
        }
    }

    return res;
}

bool CExpertBase::HasBullishSignal()
{
    return false;
}

bool CExpertBase::HasBearishSignal()
{
    return false;
}

void CExpertBase::OpenPosition(string symbol, ENUM_ORDER_TYPE orderType, double volume, double price, double stopLoss, double takeProfit)
{
    string message;
    string orderTypeMsg;

    switch (orderType) {
        case ORDER_TYPE_BUY:
            orderTypeMsg = "Buy";
            message = "Going long. Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_SELL:
            orderTypeMsg = "Sell";
            message = "Going short. Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_BUY_LIMIT:
            orderTypeMsg = "Buy limit";
            message = "Going long at " + (string)price + ". Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_SELL_LIMIT:
            orderTypeMsg = "Sell limit";
            message = "Going short at " + (string)price + ". Magic Number #" + (string)_trade.RequestMagic();
            break;
    }

    if (_inpAlertTerminalEnabled) {
        Alert(message);
    }

    if (_trade.PositionOpen(symbol, orderType, volume, price, stopLoss, takeProfit, message)) {
        uint resultCode = _trade.ResultRetcode();
        if (resultCode == TRADE_RETCODE_PLACED || resultCode == TRADE_RETCODE_DONE) {
            Print("Entry rules: A ", orderTypeMsg, " order has been successfully placed with Ticket#: ", _trade.ResultOrder());
        }
        else {
            Print("Entry rules: The ", orderTypeMsg, " order request could not be completed.  Result code: ", resultCode, ", Error: ", GetLastError());
            ResetLastError();
            return;
        }
    }
}

bool CExpertBase::IsOutsideTradingHours()
{
    MqlDateTime currentTime;
    TimeToStruct(TimeCurrent(), currentTime);
    if (_inpMinTradingHour > 0 && currentTime.hour < _inpMinTradingHour) {
        return true;
    }

    if (_inpMaxTradingHour > 0 && currentTime.hour > _inpMaxTradingHour) {
        return true;
    }

    return false;
}