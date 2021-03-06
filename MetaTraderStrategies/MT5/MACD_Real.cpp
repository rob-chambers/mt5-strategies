//+------------------------------------------------------------------+
//|                                               MACD Sample 45.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//|        Altered for 2/3/4/5 digit and ECN/STP brokers by SelectFX |
//|                                   Example intended for EURUSD H1 |
//+------------------------------------------------------------------+

#property link      "barrowboy@selectfx.net"

extern string UserSettings = "=== User Settings ===";
extern int MagicNumber = 16384;
extern double Lots = 0.1;
extern string YourOrderComment = "MACD Sample 451";

extern string SystemSettings = "=== System Settings ===";
// Change hours for broker timezone which may change with seasonal clock changes 
// More hours allowed gives more trades but may not be more profit!
extern int StartHour = 4;   // GMT 
extern int LastHour = 19;  // GMT

extern double TakeProfitLong = 50.0;     // Pip-sensitive
extern double TakeProfitShort = 75.0;     // Pip-sensitive

extern double StopLossLong = 80.0;       // Pip-sensitive 
extern double StopLossShort = 50.0;       // Pip-sensitive 

extern double TrailingStop = 30.0;   // Pip-sensitive
extern double MACDOpenLevel = 3.0;     // Pip-sensitive
extern double MACDCloseLevel = 2.0;    // Pip-sensitive
extern double MATrendPeriod = 26;


extern string BrokerSettings = "=== Broker Settings ===";
// extern bool ECN.Broker = false;  // Set to true if broker is ECN/STP needing stops adding after order
extern int Slippage = 5;         // Your choice here
extern int SleepWhenBusy = 500;  // Time in milliseconds to delay next attempt when order channel busy
extern int OrderRetries = 12;    // Number of tries to set stops if error occurs

double MacdCurrent, MacdPrevious, SignalCurrent;
double SignalPrevious, MaCurrent, MaPrevious;
int cnt, ticket, total;

static double sdPoint;
static int RealSlippage;

/*
Version History

45
Altered for 2/3/4/5 digit

451
Add handling for ECN/STP brokers
Add externals for MagicNumber and order comment
Add stop loss
Add trading hours
Add differential TP & SL for Buy/Sell

*/


#include <stderror.mqh> 

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

int init()
{
	if (Digits == 2 || Digits == 4)
	{
		sdPoint = Point;
		RealSlippage = Slippage;
	}

	if (Digits == 3 || Digits == 5)
	{
		sdPoint = Point*10.0000;
		RealSlippage = Slippage * 10;
	}

	return(0);
}

void OnTick()
{
	// initial data checks
	// it is important to make sure that the expert works with a normal
	// chart and the user did not make any mistakes setting external 
	// variables (Lots, StopLoss, TakeProfit, 
	// TrailingStop) in our case, we check TakeProfit
	// on a chart of less than 100 bars
	if (Bars < 100)
	{
		Print("bars less than 100");
		return(0);
	}

	// to simplify the coding and speed up access
	// data are put into internal variables
	MacdCurrent = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 0);
	MacdPrevious = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 1);
	SignalCurrent = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_SIGNAL, 0);
	SignalPrevious = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_SIGNAL, 1);
	MaCurrent = iMA(NULL, 0, MATrendPeriod, 0, MODE_EMA, PRICE_CLOSE, 0);
	MaPrevious = iMA(NULL, 0, MATrendPeriod, 0, MODE_EMA, PRICE_CLOSE, 1);

	total = OrdersTotal();
	if (total < 1)
	{
		// no opened orders identified
		double freeMargin = AccountFreeMargin();
		if (freeMargin < (1000 * Lots))
		{
			Print("We have no money. Free Margin = ", freeMargin);
			return(0);
		}

		// check for long position (BUY) signal
		if (MacdCurrent < 0 && MacdCurrent > SignalCurrent && MacdPrevious < SignalPrevious)
		{
			if (MathAbs(MacdCurrent) > (MACDOpenLevel * sdPoint) && MaCurrent > MaPrevious)
			{
				if (IsValidTimeToTrade() == true)
				{
					ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, RealSlippage, Ask - StopLossLong * sdPoint, Ask + TakeProfitLong * sdPoint, YourOrderComment, MagicNumber, 0, Green);
					return(0);
				}
			}
		}

		// check for short position (SELL) signal
		if (MacdCurrent > 0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious)
		{
			if (MacdCurrent > (MACDOpenLevel*sdPoint) && MaCurrent < MaPrevious)
			{
				if (IsValidTimeToTrade() == true)
				{
					ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, RealSlippage, Bid + StopLossShort*sdPoint, Bid - TakeProfitShort*sdPoint, YourOrderComment, MagicNumber, 0, Red);
				}
			}
		}

		return(0);
	}

	// it is important to enter the market correctly, 
	// but it is more important to exit it correctly...   

	/*
	for (cnt = 0; cnt < total; cnt++)
	{
		OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		if (OrderType() <= OP_SELL && OrderSymbol() == Symbol())
		{
			if (OrderType() == OP_BUY)   // long position is opened
			{
				// should it be closed?
				if (MacdCurrent > 0 && MacdCurrent < SignalCurrent && MacdPrevious > SignalPrevious)
					if (MacdCurrent > (MACDCloseLevel * sdPoint))
					{
						OrderClose(OrderTicket(), OrderLots(), Bid, RealSlippage, Violet); // close position
						return(0); // exit
					}

				// check for trailing stop
				if (TrailingStop > 0)
				{
					if (Bid - OrderOpenPrice() > sdPoint*TrailingStop)
					{
						if (OrderStopLoss() < Bid - sdPoint*TrailingStop)
						{
							OrderModify(OrderTicket(), OrderOpenPrice(), Bid - sdPoint*TrailingStop, OrderTakeProfit(), 0, Green);
							return(0);
						}
					}
				}
			}
			else // go to short position
			{
				// should it be closed?
				if (MacdCurrent < 0 && MacdCurrent > SignalCurrent)
				{
					if (MacdPrevious < SignalPrevious && MathAbs(MacdCurrent) > (MACDCloseLevel*sdPoint))
					{
						OrderClose(OrderTicket(), OrderLots(), Ask, RealSlippage, Violet); // close position
						return(0); // exit
					}
				}

				// check for trailing stop
				if (TrailingStop > 0)
				{
					if ((OrderOpenPrice() - Ask) > (sdPoint*TrailingStop))
					{
						if ((OrderStopLoss() > (Ask + sdPoint*TrailingStop)) || (OrderStopLoss() == 0))
						{
							OrderModify(OrderTicket(), OrderOpenPrice(), Ask + sdPoint*TrailingStop, OrderTakeProfit(), 0, Red);
							return(0);
						}
					}
				}
			}
		}
	}
	*/

	return(0);
}

bool IsValidTimeToTrade()
{
	// Check if OK to Open Orders
	int hour = Hour();
	if (hour >= StartHour && hour <= LastHour)
	{
		return (true);
	}

	return (false);
}

// the end.