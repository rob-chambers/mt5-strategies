//+------------------------------------------------------------------+
//|                                                        jerrold.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"

#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh> 
#include <Trade\PositionInfo.mqh>

// Money management / risk parameters
input double   _inpLots = 1;                // Number of lots to trade
input double   _inpStopLossPips = 30;       // Initial stop loss in pips
input bool     _inpUseTakeProfit = true;    // Whether to use a take profit order or not
input double   _inpTakeProfitPips = 40;     // Take profit level in pips
input int      _inpTrailingStopPips = 30;   // Trailing stop in pips (0 to not use a trailing stop)

// Pin Bar parameters
input double   _inpPinbarThreshhold = 0.6;  // Length of candle wick vs range 

// MA parameters
input bool     _inpUseMA = false;                                 // Whether to only trade based on moving average rules or not
input ENUM_TIMEFRAMES _inpMovingAveragePeriodType = PERIOD_M15;   // Moving average period
input int      _inpMovingAveragePeriodAmount = 200;               // Moving average timeframe

// Go Long / short parameters
input bool      _inpGoLong = true;          // Whether to enter long trades or not
input bool      _inpGoShort = true;         // Whether to enter short trades or not

//--- Service Variables (Only accessible from the MetaEditor)

CSymbolInfo    _symbol;                     // symbol info object
CPositionInfo  _position;                   // trade position object
CTrade _trade;
MqlRates _prices[];
double _adjustedPoints;
int _digits_adjust;
double _currentBid, _currentAsk;
int _maHandle;
double _maData[];
double _trailing_stop;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    Print("In OnInit");

    if (!_symbol.Name(Symbol())) // sets symbol name
        return(INIT_FAILED);
    
    if (!RefreshRates()) {
        Print("Could not refresh rates - init failed.");
        return(INIT_FAILED);
    }

    ArraySetAsSeries(_prices, true);
    ArraySetAsSeries(_maData, true);
    _maHandle = iMA(Symbol(), _inpMovingAveragePeriodType, _inpMovingAveragePeriodAmount, 0, MODE_SMA, PRICE_CLOSE);

    _digits_adjust = 1;
    if (_Digits == 5 || _Digits == 3 || _Digits == 1) {
        _digits_adjust = 10;
    }

    _adjustedPoints = _symbol.Point() * _digits_adjust;

    printf("DA=%f, adjusted points = %f", _digits_adjust, _adjustedPoints);

    _trailing_stop = _inpTrailingStopPips * _adjustedPoints;

    return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    Print("In OnDeinit for reason: ", reason);   
    ReleaseIndicator(_maHandle);
}

void ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    //--- we work only at the time of the birth of new bar
    static datetime PrevBars = 0;
    datetime time_0 = iTime(0);
    if (time_0 == PrevBars)
        return;

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
    int maDataCount = CopyBuffer(_maHandle, 0, 0, 3, _maData);

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

//+------------------------------------------------------------------+ 
//| Get Time for specified bar index                                 | 
//+------------------------------------------------------------------+ 
datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT)
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
bool RefreshRates()
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

bool HasBullishSignal()
{
    /* Rules:
    Current candle high > previous candle high
    Current candle close < previous candle high
    Current (high-close) / (high-low) > 0.6 and (high - open) / (high-low) > 0.6
    Current low > previous low 

    Close must be above moving average (200 period by default)
    */
    if (_prices[1].high <= _prices[2].high) return false;
    if (_prices[1].close >= _prices[2].high) return false;

    double currentRange = _prices[1].high - _prices[1].low;
    if (!((_prices[1].high - _prices[1].close) / currentRange > _inpPinbarThreshhold &&
        (_prices[1].high - _prices[1].open) / currentRange > _inpPinbarThreshhold)) {
        return false;
    }

    if (_prices[1].low <= _prices[2].low) return false;

    bool maSignal = false;
    if (!_inpUseMA) {
        // Ignore if we don't care
        maSignal = true;
    }
    else {
        maSignal = _prices[1].close > _maData[0];
    }

    return maSignal;
}

bool HasBearishSignal()
{   
    /* Rules:
    Current candle low < previous candle low
    Current candle close < previous candle low
    Current (high-close) / (high-low) > 0.6 and (high - open) / (high-low) > 0.6
    Current high < previous high
    
    Price must be below moving average (200 period by default)
    */
    if (_prices[1].close >= _prices[1].open) return false;
    if (_prices[1].low >= _prices[2].low) return false;
    if (_prices[1].close >= _prices[2].low) return false;

    double currentRange = _prices[1].high - _prices[1].low;
    if (!((_prices[1].close - _prices[1].low) / currentRange > _inpPinbarThreshhold)) return false;

    if (_prices[1].high > _prices[2].high) return false;

    bool maSignal = false;
    if (!_inpUseMA) {
        // Ignore if we don't care
        maSignal = true;
    }
    else {
        maSignal = _prices[1].close < _maData[0];
    }

    return maSignal;
}

void OpenPosition(string symbol, ENUM_ORDER_TYPE orderType, double volume, double price, double stopLoss, double takeProfit)
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

bool CheckToModifyPositions()
{
    if (_inpTrailingStopPips == 0) return false;
    if (_position.Select(Symbol())) {
        if (_position.PositionType() == POSITION_TYPE_BUY) {
            //--- try to close or modify long position
            /*if (LongClosed())
                return(true);*/
            if (LongModified())
                return(true);
        }
        else {
            //--- try to close or modify short position
            /*if (ShortClosed())
                return(true);*/
            /*if (ShortModified())
                return(true);*/
        }
    }

    return false;
}

bool LongModified()
{
    bool res = false;
    if (_inpTrailingStopPips <= 0) return false;
    
    if (_symbol.Bid() - _position.PriceOpen() > _trailing_stop) {
        double sl = NormalizeDouble(_symbol.Bid() - _trailing_stop, _symbol.Digits());
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

    return (res);
}
//+------------------------------------------------------------------+
