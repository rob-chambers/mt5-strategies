//+------------------------------------------------------------------+
//|                                                           star.mq5 
//|                                    Copyright 2017, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Robert Chambers"
#property version   "1.00"

#include "CStar.mqh"

//+------------------------------------------------------------------+
//| Input variables                                  |
//+------------------------------------------------------------------+
input double            _inpLots = 1;                // Number of lots to trade
input STOPLOSS_RULE     _inpInitialStopLossRule = StaticPipsValue; // Initial Stop Loss Rule
input int               _inpInitialStopLossPips = 20;       // Initial stop loss in pips
input bool              _inpUseTakeProfit = true;    // Whether to use a take profit order or not
input int               _inpTakeProfitPips = 50;     // Take profit level in pips
input STOPLOSS_RULE     _inpTrailingStopLossRule = None; // Trailing Stop Loss Rule
input int               _inpTrailingStopPips = 0;   // Trailing stop in pips (0 to not use a trailing stop)
input int               _inpMinutesToWaitAfterPositionClosed = 60;   // Number of minutes to wait before a new signal is raised after the last position was closed

// Go Long / short parameters
input bool              _inpGoLong = true;          // Whether to enter long trades or not
input bool              _inpGoShort = true;         // Whether to enter short trades or not

// Alert parameters
input bool              _inpAlertTerminalEnabled = true;  // Whether to show terminal alerts or not
input bool              _inpAlertEmailEnabled = false;  // Whether to alert via email or not

// Trading time parameters
input int               _inpMinTradingHour = 0;     // The minimum hour of the day to trade (e.g. 7 for 7am)
input int               _inpMaxTradingHour = 0;    // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Slow moving average parameters
input bool              _inpFilterByMA = false;
input ENUM_TIMEFRAMES   _inpMAPeriodType = PERIOD_D1;
input int               _inpMAPeriodAmount = 8;

CStar derived;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    return derived.Init(
        _inpLots,
        _inpInitialStopLossRule,
        _inpInitialStopLossPips,
        _inpUseTakeProfit,
        _inpTakeProfitPips,
        _inpTrailingStopLossRule,
        _inpTrailingStopPips,
        _inpGoLong,
        _inpGoShort,
        _inpAlertTerminalEnabled,
        _inpAlertEmailEnabled,
        _inpMinutesToWaitAfterPositionClosed,
        _inpMinTradingHour,
        _inpMaxTradingHour,
        _inpFilterByMA,
        _inpMAPeriodType,
        _inpMAPeriodAmount
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