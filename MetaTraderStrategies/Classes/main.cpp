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
input int      _inpTrailingStopPips = 30;   // Trailing stop in pips (0 to not use a trailing stop)


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
        _inpTrailingStopPips);
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