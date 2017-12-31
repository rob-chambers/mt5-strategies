//+------------------------------------------------------------------+
//|                                                        main.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"

#include "CDerived.mqh"

//+------------------------------------------------------------------+
//| Input variables                                  |
//+------------------------------------------------------------------+
input double   _inpLots = 1;                // Number of lots to trade
input double   _inpStopLossPips = 30;       // Initial stop loss in pips
input bool     _inpUseTakeProfit = true;    // Whether to use a take profit order or not
input double   _inpTakeProfitPips = 40;     // Take profit level in pips
input int      _inpTrailingStopPips = 0;   // Trailing stop in pips (0 to not use a trailing stop)

// Go Long / short parameters
input bool      _inpGoLong = true;          // Whether to enter long trades or not
input bool      _inpGoShort = true;         // Whether to enter short trades or not

// Pin Bar parameters
input double   _inpPinbarThreshhold = 0.6;  // Length of candle wick vs range
input double   _inpPinbarRangeThreshhold = 2; // Range of pin bar compared to historical range

CDerived derived;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    return derived.Init(
        _inpLots,
        _inpStopLossPips,
        _inpUseTakeProfit,
        _inpTakeProfitPips,
        _inpTrailingStopPips,
        _inpGoLong,
        _inpGoShort,
        _inpPinbarThreshhold,
        _inpPinbarRangeThreshhold);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    derived.Deinit();
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    derived.Processing();
}