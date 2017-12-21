//+------------------------------------------------------------------+
//|                                                        jerrold.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"
/*
Rules:
Current candle high > previous candle high
Current candle close < previous candle high
Current (high-close) / (high-low) > 0.6 and (high - open) / (high-low) > 0.6
Current low > previous low
*/

#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh> 

//--- Input Variables (Accessible from MetaTrader 5)

// Money management / risk parameters
input double   _lots = 1;
input int      _slippage = 2;
input double   _stopLossPips = 10;
input bool     _useTakeProfit = true;
input double   _takeProfitPips = 15;
input double   _pinbarThreshhold = 0.6;
input int       _timeout = 10;

//--- Service Variables (Only accessible from the MetaEditor)

CSymbolInfo    m_symbol;                     // symbol info object
CTrade _trade;
MqlRates _prices[];
int _adjustedPoints;
double _currentBid, _currentAsk;
int _lastTradeTime;
static datetime _limit_time = 0;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    if (!m_symbol.Name(Symbol())) // sets symbol name
        return(INIT_FAILED);
    
    if (!RefreshRates()) {
        Print("Could not refresh rates - init failed.");
        return(INIT_FAILED);
    }

    ArraySetAsSeries(_prices, true);

    if (_Digits == 5 || _Digits == 3 || _Digits == 1) {
        _adjustedPoints = 10;
    }
    else {
        _adjustedPoints = 1; // To account for 5 digit brokers
    }

    return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    Print("In OnDeinit for reason: ", reason);   
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

    Print("New bar");
    PrevBars = time_0;

    double stopLossPipsFinal;
    double takeProfitPipsFinal;
    double stopLossLevel;
    double takeProfitLevel;
    double stopLevelPips;

    //--- we work only at the time of the birth of new bar
    //datetime time_0 = iTime(0);
    //if (time_0 == PrevBars)
    //    return;
    //PrevBars = time_0;
    //---

    // -------------------- Collect most current data --------------------
    if (!RefreshRates()) {
        return;
    }

    int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 10, _prices); // Collects data from shift 0 to shift 9

    // -------------------- EXITS --------------------

    if (PositionSelect(_Symbol) == true) // We have an open position
    {
        return;
    }

    // -------------------- ENTRIES --------------------  
    if (PositionSelect(_Symbol) == false) // We have no open positions
    {
        if (!HasMoney()) {
            return;
        }

        double limitPrice;
        if (HasBullishSignal()) {
            int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 10, _prices); // Collects data from shift 0 to shift 9

            stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _adjustedPoints; // Defining minimum StopLevel
            if (_stopLossPips < stopLevelPips) {
                stopLossPipsFinal = stopLevelPips;
            }
            else {
                stopLossPipsFinal = _stopLossPips;
            }

            if (_takeProfitPips < stopLevelPips) {
                takeProfitPipsFinal = stopLevelPips;
            }
            else {
                takeProfitPipsFinal = _takeProfitPips;
            }

            limitPrice = _currentAsk;
            stopLossLevel = limitPrice - stopLossPipsFinal * _Point * _adjustedPoints;
            if (_useTakeProfit) {
                takeProfitLevel = limitPrice + takeProfitPipsFinal * _Point * _adjustedPoints;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_BUY, _lots, limitPrice, stopLossLevel, takeProfitLevel);
        }
        else if (HasBearishSignal()) {
            limitPrice = _currentBid;

            stopLossLevel = limitPrice + stopLossPipsFinal * _Point * _adjustedPoints;
            if (_useTakeProfit) {
                takeProfitLevel = limitPrice - takeProfitPipsFinal * _Point * _adjustedPoints;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_SELL, _lots, limitPrice, stopLossLevel, takeProfitLevel);
        }
    }
}

//+------------------------------------------------------------------+ 
//| Get Time for specified bar index                                 | 
//+------------------------------------------------------------------+ 
datetime iTime(const int index, string symbol = NULL, ENUM_TIMEFRAMES timeframe = PERIOD_CURRENT)
{
    if (symbol == NULL)
        symbol = m_symbol.Name();
    if (timeframe == 0)
        timeframe = Period();
    datetime Time[1];
    datetime time = 0;
    int copied = CopyTime(symbol, timeframe, index, 1, Time);
    if (copied > 0) time = Time[0];
    return(time);
}

// Check if we have enough money to make a trade
bool HasMoney()
{
    // TODO: I guess AccountFreeMargin is an MT4 function
    // Program equivalent in MT5

    /*
    double freeMargin = AccountFreeMargin();
    if (freeMargin < (1000 * _lots))
    {
        Print("We have no money. Free Margin = ", freeMargin);
        return false;
    }
    */

    return true;
}

//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates()
{
    //--- refresh rates
    if (!m_symbol.RefreshRates())
        return(false);
    //--- protection against the return value of "zero"
    if (m_symbol.Ask() == 0 || m_symbol.Bid() == 0)
        return(false);
    //---

    _currentBid = m_symbol.Bid();
    _currentAsk = m_symbol.Ask();

    return(true);
}

bool HasBullishSignal()
{
    /*
    Rules:
    Current candle high > previous candle high
    Current candle close < previous candle high
    Current (high-close) / (high-low) > 0.6 and (high - open) / (high-low) > 0.6
    Current low > previous low
    */
    if (_prices[1].high <= _prices[2].high) return false;
    if (_prices[1].close >= _prices[2].high) return false;

    double currentRange = _prices[1].high - _prices[1].low;
    if (!((_prices[1].high - _prices[1].close) / currentRange > _pinbarThreshhold &&
        (_prices[1].high - _prices[1].open) / currentRange > _pinbarThreshhold)) {
        return false;
    }

    if (_prices[1].low <= _prices[2].low) return false;

    return true;
}

bool HasBearishSignal()
{   
    return false;
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
            //_lastTradeTime = Now();
            //Print("Trade placed at ", (string)_lastTradeTime);
        }
        else {
            Print("Entry rules: The ", orderTypeMsg, " order request could not be completed.  Result code: ", resultCode, ", Error: ", GetLastError());
            ResetLastError();
            return;
        }
    }
}
//+------------------------------------------------------------------+
