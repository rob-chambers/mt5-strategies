//+------------------------------------------------------------------+
//|                                                        PinBars.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"
/*
Rules:

Look for a pin bar
Enter trade on the open of the very next bar.  Set stop to risk amount
Set target profit as a multiple of risk amount (default to 1.5)
Default risk of 25 pips (hard stop)

For going long, ensure price > 200 day MA
For going short, ensure price < 200 day MA

For going long, ensure RSI < x
For going short, ensure RSI > x

Colour of pin head does not matter!
Pin size = 1.5x?

Determine pin type to look out for by checking 21 period MA

Another rule perhaps?  Once trade entered, if not in profit by 5 periods, exit?


Use input variables UseStopLoss / _useTakeProfit

If (PositionSelect(symbol) == false)…
Check current direction of position: if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)…


Entries:
- Enter a long trade when SMA(10) crosses SMA(40) from bottom if High[1] - Low[1] is greater than High[2] - Low[2]
- Enter a short trade when SMA(10) crosses SMA(40) from top if High[1] - Low[1] is greater than High[2] - Low[2]

Exits:
- Exit the long trade when SMA(10) crosses SMA(40) from top
- Exit the short trade when SMA(10) crosses SMA(40) from bottom
- 30 pips hard stop (30pips from initial entry price)

Position Sizing
   - Enter 1 _lots at a time
   - Maximum number of open positions = 1

Notes (for the more advanced MQL5 traders):
   - We are using Hedging accounts. Thus, we are doing order-centric trade management.

*/

#include <Trade\Trade.mqh>

//--- Input Variables (Accessible from MetaTrader 5)

input double   _lots = 2;
input int      _slippage = 3;
input double   _stopLossPips = 25;
input bool     _useTakeProfit = true;
input double   _takeProfitPips = 40;
input double   _pinBarLengthPercent = 20;

//--- Service Variables (Only accessible from the MetaEditor)

CTrade _trade;
MqlRates _prices[];
int _adjustedPoints;
double _currentBid, _currentAsk;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    ArraySetAsSeries(_prices, true); // Setting up table/array for time series data
    //ArraySetAsSeries(shortSmaData, true);   // Setting up table/array for time series data
    //ArraySetAsSeries(longSmaData, true);    // Setting up table/array for time series data

    //shortSmaControlPanel = iMA(_Symbol, _Period, shortSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for short SMA
    //longSmaControlPanel = iMA(_Symbol, _Period, longSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for long SMA

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
    //---
    /*IndicatorRelease(shortSmaControlPanel);
    IndicatorRelease(longSmaControlPanel);*/
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    double stopLossPipsFinal;
    double takeProfitPipsFinal;
    double stopLossLevel;
    double takeProfitLevel;
    double stopLevelPips;

    // -------------------- Collect most current data --------------------

    _currentBid = SymbolInfoDouble(_Symbol, SYMBOL_BID); // Get latest Bid Price
    _currentAsk = SymbolInfoDouble(_Symbol, SYMBOL_ASK); // Get latest Ask Price

    int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 10, _prices); // Collects data from shift 0 to shift 9
    //numberOfShortSmaData = CopyBuffer(shortSmaControlPanel, 0, 0, 3, shortSmaData); // Collect most current SMA(10) Data and store it in the datatable/array shortSmaData[]
    //numberOfLongSmaData = CopyBuffer(longSmaControlPanel, 0, 0, 3, longSmaData); // Collect most current SMA(40) Data and store it in the datatable/array longSmaData[]

    // TODO: Check for errors from above calls   

    // -------------------- Technical Requirements --------------------

    // Explanation: Stop Loss and Take Profit levels can't be too close to our order execution price. We will talk about this again in a later lecture.
    // Resources for learning more: https://book.mql4.com/trading/orders (ctrl-f search "stoplevel"); https://book.mql4.com/appendix/limits

    stopLevelPips = (double)(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / _adjustedPoints; // Defining minimum StopLevel
    if (_stopLossPips < stopLevelPips)
    {
        stopLossPipsFinal = stopLevelPips;
    }
    else
    {
        stopLossPipsFinal = _stopLossPips;
    }

    if (_takeProfitPips < stopLevelPips)
    {
        takeProfitPipsFinal = stopLevelPips;
    }
    else
    {
        takeProfitPipsFinal = _takeProfitPips;
    }

    // -------------------- EXITS --------------------

    if (PositionSelect(_Symbol) == true) // We have an open position
    {
        ManageExistingPositions();        
    }

    // -------------------- ENTRIES --------------------  
    if (PositionSelect(_Symbol) == false) // We have no open positions
    {
        /* --- Entry Rules (Long Trades)
        1) Pin Bar
        2) 
        */
        if (HasBullishSignal())
        {
            stopLossLevel = _currentAsk - stopLossPipsFinal * _Point * _adjustedPoints;
            if (_useTakeProfit) {
                takeProfitLevel = _currentAsk + takeProfitPipsFinal * _Point * _adjustedPoints;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_BUY, _lots, _currentAsk, stopLossLevel, takeProfitLevel);            
        }

        //// Entry rule for short trades
        //else if (_prices[1].high - _prices[1].low > _prices[2].high - _prices[2].low &&
        //    shortSma2 > longSma2 && shortSma1 <= longSma1)
        //{

        //    if (useStopLoss) stopLossLevel = _currentBid + stopLossPipsFinal * _Point * _adjustedPoints; else stopLossLevel = 0.0;
        //    if (_useTakeProfit) takeProfitLevel = _currentBid - takeProfitPipsFinal * _Point * _adjustedPoints; else takeProfitLevel = 0.0;

        //    _trade.PositionOpen(_Symbol, ORDER_TYPE_SELL, _lots, _currentBid, stopLossLevel, takeProfitLevel, "Sell Trade. Magic Number #" + (string)_trade.RequestMagic()); // Open a Sell position

        //    if (_trade.ResultRetcode() == 10008 || _trade.ResultRetcode() == 10009) //Request is completed or order placed
        //    {
        //        Print("Entry rules: A Sell order has been successfully placed with Ticket#: ", _trade.ResultOrder());
        //    }
        //    else
        //    {
        //        Print("Entry rules: The Sell order request could not be completed.Error: ", GetLastError());
        //        ResetLastError();
        //        return;
        //    }
        //}
    }
}

bool HasBullishSignal()
{
    return IsBullishPinBar(_prices[1].open, _prices[1].high, _prices[1].low, _prices[1].close);
}

bool IsBullishPinBar(double open, double high, double low, double close)
{
    double range = high - low;
    double closeOpenRange = MathAbs(open - close);
    double pinBarPct = _pinBarLengthPercent / 100;

    if (range * pinBarPct >= closeOpenRange) {
        Print("Pin bar found. OHLC = ", (string)open + ",", (string)high, ",", (string)low, ",", (string)close);

        double x = (high - close) / range;
        Print("x = ", (string)x, ", pinBarPct = ", (string)pinBarPct);
        if (x < pinBarPct) {
            return true;
        }
    }

    return false;
}

void ManageExistingPositions()
{
    // --- Exit Rules (Long Trades) ---        
    //if () // Rule to exit long trades
    //{
    //    if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) // If it is Buy position
    //    {
    //        _trade.PositionClose(_Symbol);

    //        if (_trade.ResultRetcode() == 10008 || _trade.ResultRetcode() == 10009) //Request is completed or order placed
    //        {
    //            Print("Exit rules: A close order has been successfully placed with Ticket#: ", _trade.ResultOrder());
    //        }
    //        else
    //        {
    //            Print("Exit rules: The close order request could not be completed. Error: ", GetLastError());
    //            ResetLastError();
    //            return;
    //        }
    //    }
    //}
}

void OpenPosition(string symbol, ENUM_ORDER_TYPE orderType, double volume, double price, double stopLoss, double takeProfit)
{
    string message;
    string orderTypeMsg;

    switch (orderType) {
        case ORDER_TYPE_BUY:
            orderTypeMsg = "Buy";
            message = "Buy Trade. Magic Number #" + (string)_trade.RequestMagic();
            break;

        case ORDER_TYPE_SELL:
            orderTypeMsg = "Sell";
            message = "Sell Trade. Magic Number #" + (string)_trade.RequestMagic();
            break;
    }

    _trade.PositionOpen(symbol, orderType, volume, price, stopLoss, takeProfit, message);
    if (_trade.ResultRetcode() == 10008 || _trade.ResultRetcode() == 10009)
    {
        // Request is completed or order placed
        Print("Entry rules: A ", orderTypeMsg, " order has been successfully placed with Ticket#: ", _trade.ResultOrder());
    }
    else
    {
        Print("Entry rules: The ", orderTypeMsg, " order request could not be completed. Error: ", GetLastError());
        ResetLastError();
        return;
    }
}

//+------------------------------------------------------------------+
