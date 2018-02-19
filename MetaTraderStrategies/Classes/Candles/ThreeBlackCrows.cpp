//+------------------------------------------------------------------+
//|                                                threeblackcrows.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.00"

#include "CThreeBlackCrows.mqh"

//+------------------------------------------------------------------+
//| Input variables                                  |
//+------------------------------------------------------------------+
input double   _inpLots = 1;                // Number of lots to trade
input STOPLOSS_RULE _inpStopLossRule = StaticPipsValue; // Stop Loss Rule
input double   _inpStopLossPips = 15;       // Initial stop loss in pips
input bool     _inpUseTakeProfit = true;    // Whether to use a take profit order or not
input double   _inpTakeProfitPips = 30;     // Take profit level in pips
input int      _inpTrailingStopPips = 20;   // Trailing stop in pips (0 to not use a trailing stop)
input int      _inpMinutesToWaitAfterPositionClosed = 60;   // Number of minutes to wait before a new signal is raised after the last position was closed

// Go Long / short parameters
input bool      _inpGoLong = false;          // Whether to enter long trades or not
input bool      _inpGoShort = true;         // Whether to enter short trades or not

// Alert parameters
input bool      _inpAlertTerminalEnabled = true;  // Whether to show terminal alerts or not
input bool      _inpAlertEmailEnabled = false;  // Whether to alert via email or not

// Trading time parameters
input int       _inpMinTradingHour = 7;     // The minimum hour of the day to trade (e.g. 7 for 7am)
input int       _inpMaxTradingHour = 19;    // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Technical parameters
input int       _inpMAPeriod = 12;          // The number of bars used for averaging

CThreeBlackCrows derived;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    return derived.Init(
        _inpLots,
        _inpStopLossRule,
        _inpStopLossPips,
        _inpUseTakeProfit,
        _inpTakeProfitPips,
        _inpTrailingStopPips,
        _inpGoLong,
        _inpGoShort,
        _inpAlertTerminalEnabled,
        _inpAlertEmailEnabled,
        _inpMinutesToWaitAfterPositionClosed,
        _inpMinTradingHour,
        _inpMaxTradingHour,
        _inpMAPeriod
    );
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