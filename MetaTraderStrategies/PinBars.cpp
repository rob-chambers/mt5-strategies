//+------------------------------------------------------------------+
//|                                                        PinBars.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"
/*
Rules:

Look for a pin bar
Only go long when the RSI < 45
Only go short when RSI > 55
Enter trade on the open of the very next bar.  Set stop to risk amount
Set target profit to hard value (default of 40 pips)
Default risk of 25 pips (hard stop)

For going long, ensure price > 200 day MA
For going short, ensure price < 200 day MA

For going long, ensure RSI < x
For going short, ensure RSI > x

Colour of pin head does not matter!
Pin candle length must be at least the same as the Average True Range (configure using _pinCandleBodyLengthMinimumMultiple parameter)

Only go long when price is higher than the 21 HOUR MA
Only go short when price is lower than the 21 HOUR MA

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
input double   _pinBarLengthPercent = 33;
input int      _rsiPeriod = 14;
input int      _rsiLongThreshold = 45;
input int      _rsiShortThreshold = 55;
input int      _atrPeriod = 14;
input double   _pinCandleBodyLengthMinimumMultiple = 1;
input ENUM_TIMEFRAMES _movingAveragePeriodType = PERIOD_H1;
input int      _movingAveragePeriodAmount = 21;

//--- Service Variables (Only accessible from the MetaEditor)

CTrade _trade;
MqlRates _prices[];
int _adjustedPoints;
double _currentBid, _currentAsk;
int _rsiHandle, _atrHandle, _maHandle;
double _rsiData[], _atrData[], _maData[];

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    ArraySetAsSeries(_prices, true);
    ArraySetAsSeries(_rsiData, true);
    ArraySetAsSeries(_atrData, true);
    ArraySetAsSeries(_maData, true);

    //ArraySetAsSeries(longSmaData, true);    // Setting up table/array for time series data

    //shortSmaControlPanel = iMA(_Symbol, _Period, shortSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for short SMA
    //longSmaControlPanel = iMA(_Symbol, _Period, longSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for long SMA
    _rsiHandle = iRSI(_Symbol, _Period, _rsiPeriod, PRICE_CLOSE);
    _atrHandle = iATR(_Symbol, 0, _atrPeriod);
    _maHandle = iMA(_Symbol, _movingAveragePeriodType, _movingAveragePeriodAmount, 0, MODE_SMA, PRICE_CLOSE);

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
    
    ReleaseIndicator(_rsiHandle);
    ReleaseIndicator(_atrHandle);
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
    int rsiDataCount = CopyBuffer(_rsiHandle, 0, 0, 3, _rsiData);
    int atrDataCount = CopyBuffer(_atrHandle, 0, 0, 3, _atrData);
    int maDataCount = CopyBuffer(_maHandle, 0, 0, 3, _maData);

    // TODO: Check for errors from above calls   

    // -------------------- Technical Requirements --------------------

    // Explanation: Stop Loss and Take Profit levels can't be too close to our order execution price. We will talk about this again in a later lecture.
    // Resources for learning more: https://book.mql4.com/trading/orders (ctrl-f search "stoplevel"); https://book.mql4.com/appendix/limits

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
        if (HasBullishSignal()) {
            stopLossLevel = _currentAsk - stopLossPipsFinal * _Point * _adjustedPoints;
            if (_useTakeProfit) {
                takeProfitLevel = _currentAsk + takeProfitPipsFinal * _Point * _adjustedPoints;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_BUY, _lots, _currentAsk, stopLossLevel, takeProfitLevel);            
        }
        else if (HasBearishSignal()) {
            stopLossLevel = _currentAsk + stopLossPipsFinal * _Point * _adjustedPoints;
            if (_useTakeProfit) {
                takeProfitLevel = _currentAsk - takeProfitPipsFinal * _Point * _adjustedPoints;
            }
            else {
                takeProfitLevel = 0.0;
            }

            OpenPosition(_Symbol, ORDER_TYPE_SELL, _lots, _currentAsk, stopLossLevel, takeProfitLevel);
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
    bool isBar = IsBullishPinBar(_prices[1].open, _prices[1].high, _prices[1].low, _prices[1].close);

    if (isBar) {
        if (_rsiData[1] < _rsiLongThreshold) {            
            if (_prices[1].close > _maData[0]) {
                return true;
            }
            else {
                Print("LONG trade rejected due to MA...Price: ", _prices[1].close, ", MA: ", _maData[0]);
            }
        }
    }
    
    return false;
}

bool HasBearishSignal()
{
    bool isBar = IsBearishPinBar(_prices[1].open, _prices[1].high, _prices[1].low, _prices[1].close);

    if (isBar) {
        if (_rsiData[1] > _rsiShortThreshold) {
            if (_prices[1].close < _maData[0]) {
                return true;
            }
            else {
                Print("SHORT trade rejected due to MA...Price: ", _prices[1].close, ", MA: ", _maData[0]);
            }
        }
    }

    return false;
}

bool IsBullishPinBar(double open, double high, double low, double close)
{
    double range = high - low;
    double headSize = MathAbs(open - close);
    double pinBarPct = _pinBarLengthPercent / 100;

    if (range * pinBarPct >= headSize) {
        //Print("Pin bar found. OHLC = ", (string)open + ",", (string)high, ",", (string)low, ",", (string)close);

        //Print("x = ", (string)x, ", pinBarPct = ", (string)pinBarPct);

        // Ensure length of candle shadow is sufficiently small
        if ((high - close) <= headSize / 2) {
            // Now ensure candle wick is of a sufficient length
            if (range >= _pinCandleBodyLengthMinimumMultiple * _atrData[0]) {
                return true;
            }
        }
    }

    return false;
}

bool IsBearishPinBar(double open, double high, double low, double close)
{
    double range = high - low;
    double headSize = MathAbs(open - close);
    double pinBarPct = _pinBarLengthPercent / 100;

    if (range * pinBarPct >= headSize) {
        // Ensure length of candle shadow is sufficiently small
        if ((close - low) <= headSize / 2) {
            // Now ensure candle wick is of a sufficient length
            if (range >= _pinCandleBodyLengthMinimumMultiple * _atrData[0]) {
                return true;
            }
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

//+------------------------------------------------------------------+
