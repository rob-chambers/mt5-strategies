//+------------------------------------------------------------------+
//|                                                        newtrend.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.00"

#include "CNewTrend.mqh"

//+------------------------------------------------------------------------------------------------------------------------------+
//| Input variables                                                                                                              |
//+------------------------------------------------------------------------------------------------------------------------------+
input double   _inpLots = 1;                                    // Number of lots to trade
input STOPLOSS_RULE _inpInitialStopLossRule = CurrentBarNPips;  // Initial Stop Loss Rule
input int   _inpInitialStopLossPips = 3;                        // Initial stop loss in pips
input bool     _inpUseTakeProfit = false;                       // Whether to use a take profit order or not
input int   _inpTakeProfitPips = 0;                             // Take profit level in pips
input double _inpTakeProfitRiskRewardRatio = 0;                 // Risk/Reward ratio used for take profit order
input STOPLOSS_RULE _inpTrailingStopLossRule = None;            // Trailing Stop Loss Rule
input int      _inpTrailingStopPips = 0;                        // Trailing stop in pips (0 to not use a trailing stop)
input bool     _inpMoveToBreakEven = true;                      // Trail stop to break even position
input int      _inpMinutesToWaitAfterPositionClosed = 0;        // Time to wait before trading again after last position closed

// Go Long / short parameters
input bool      _inpGoLong = true;                              // Whether to enter long trades or not
input bool      _inpGoShort = true;                             // Whether to enter short trades or not

// Alert parameters
input bool      _inpAlertTerminalEnabled = true;                // Whether to show terminal alerts or not
input bool      _inpAlertEmailEnabled = false;                  // Whether to alert via email or not

// Trading time parameters
input int       _inpMinTradingHour = 0;                         // The minimum hour of the day to trade (e.g. 7 for 7am)
input int       _inpMaxTradingHour = 0;                         // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Pin Bar parameters
//input double   _inpPinbarThreshhold = 0.6;  // Length of candle wick vs range
//input double   _inpPinbarRangeThreshhold = 2; // Range of pin bar compared to historical range

// Technical parameters
input int       _inpBarCountHighestHigh = 40; // The number of bars where the current high must be higher than
input bool      _inpFilterByADX = true;     // Whether to take into account the ADX indicator or not when providing signals
input int       _inpADXPeriod = 14;         // The number of bars used to calculate the ADX
input int       _inpBarCountInRange = 10;   // The number of bars that must have an ADX value lower than the threshold
input int       _inpADXThreshold = 30;      // The ADX threshold value used to determine whether we are in a range or not

input bool      _inpFilterByMA = true;              // Whether to take into account the Moving Average rule
input ENUM_TIMEFRAMES _inpMAPeriodType = PERIOD_H1; // The time frame of the moving average
input int _inpMAPeriodAmount = 21;                  // The number of bars used for calculating the Moving Average

CNewTrend derived;

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
        _inpTakeProfitRiskRewardRatio,
        _inpTrailingStopLossRule,
        _inpTrailingStopPips,
        _inpMoveToBreakEven,
        _inpGoLong,
        _inpGoShort,
        _inpAlertTerminalEnabled,
        _inpAlertEmailEnabled,
        _inpMinutesToWaitAfterPositionClosed,
        _inpMinTradingHour,
        _inpMaxTradingHour,
        _inpBarCountHighestHigh,
        _inpFilterByADX,
        _inpADXPeriod,
        _inpBarCountInRange,
        _inpADXThreshold,
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