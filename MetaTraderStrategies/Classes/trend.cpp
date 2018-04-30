//+------------------------------------------------------------------+
//|                                                          trend.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.00"

#include "CTrend.mqh"

//+------------------------------------------------------------------------------------------------------------------------------+
//| Input variables                                                                                                              |
//+------------------------------------------------------------------------------------------------------------------------------+
input double        _inpDynamicSizingRiskPerTrade = 1;              // Risk per trade of account balance
input STOPLOSS_RULE _inpInitialStopLossRule = CurrentBarNPips;      // Initial Stop Loss Rule
input int           _inpInitialStopLossPips = 4;                    // Initial stop loss in pips
input bool          _inpUseTakeProfit = false;                      // Whether to use a take profit order or not
input int           _inpTakeProfitPips = 0;                         // Take profit level in pips
input double        _inpTakeProfitRiskRewardRatio = 0;              // Risk/Reward ratio used for take profit order
input STOPLOSS_RULE _inpTrailingStopLossRule = None;                // Trailing Stop Loss Rule
input int           _inpTrailingStopPips = 0;                       // Trailing stop in pips (0 to not use a trailing stop)
input bool          _inpMoveToBreakEven = true;                     // Trail stop to break even position

// Go Long / short parameters
input bool      _inpGoLong = true;                                  // Whether to enter long trades or not
input bool      _inpGoShort = true;                                 // Whether to enter short trades or not

// Alert parameters
input bool      _inpAlertTerminalEnabled = true;                    // Whether to show terminal alerts or not
input bool      _inpAlertEmailEnabled = false;                      // Whether to alert via email or not

// Trading time parameters
input int       _inpMinTradingHour = 0;                             // The minimum hour of the day to trade (e.g. 7 for 7am)
input int       _inpMaxTradingHour = 0;                             // The maximum hour of the day to trade (e.g. 19 for 7pm)

// Technical parameters
input int       _inpLongTermPeriod = 70;                            // The number of bars on the long term timeframe used to determine the trend
input int       _inpShortTermPeriod = 25;                           // The number of bars on the short term timeframe used to determine the trend
input double    _inpShortTermTrendRejectionMultiplier = 1.5;

input double    _inpStrongTrendThreshold = 2;
input double    _inpStandardTrendThreshold = 0.4;
input double    _inpWeakTrendThreshold = 0.2;

CTrend derived;

//+------------------------------------------------------------------+
//| Expert initialisation function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    return derived.Init(
        Dynamic,
        _inpDynamicSizingRiskPerTrade,
        0,
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
        0,
        _inpMinTradingHour,
        _inpMaxTradingHour,
        _inpLongTermPeriod,
        _inpShortTermPeriod,
        _inpShortTermTrendRejectionMultiplier,
        _inpStrongTrendThreshold,
        _inpStandardTrendThreshold,
        _inpWeakTrendThreshold
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

//+------------------------------------------------------------------+
//| Expert trade event
//+------------------------------------------------------------------+
void OnTrade()
{
    derived.OnTrade();
}