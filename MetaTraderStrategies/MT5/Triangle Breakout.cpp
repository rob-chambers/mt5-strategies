//#property link      "barrowboy@selectfx.net"
extern int MagicNumber = 678543;
extern double Lots = 0.1;
extern string YourOrderComment = "Triangle Breakout";

// Change hours for broker timezone which may change with seasonal clock changes 
extern int StartHour = 0;
extern int LastHour = 24;
extern int MaxRange = 380;              // Maximum Trading Range (Points)
extern int MinRange = 110;              // Minimum Trading Range (Points)
extern int BreakRange = 10;             // Break Of Trading Range (Points)

enum ENUM_POS_DAY
{
	PosLongOnly,      // Long Only
	PosShortOnly,     // Short Only
	PosLongShort,     // Long & Short
	PosNo             // None
};


#include <stderror.mqh> 
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
	// chart and the user did not make any mistakes setting external variables
	if (Bars < 100)
	{
		Print("bars less than 100");
		return;
	}

	// to simplify the coding and speed up access
	// data are put into internal variables
	
	if (IsOpenOrder())
	{
		return;
	}

	// no opened orders identified
	double freeMargin = AccountFreeMargin();
	if (freeMargin < (1000 * Lots))
	{
		Print("We have no money. Free Margin = ", freeMargin);
		return;
	}

	// check for long position (BUY) signal
	if (BuySignal())
	{
		if (IsValidTimeToTrade() == true)
		{
			ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, RealSlippage, Ask - StopLossLong * sdPoint, Ask + TakeProfitLong * sdPoint, YourOrderComment, MagicNumber, 0, Green);
			return;
		}
	}

	// check for short position (SELL) signal
	if (SellSignal())
	{
		if (IsValidTimeToTrade() == true)
		{
			ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, RealSlippage, Bid + StopLossShort*sdPoint, Bid - TakeProfitShort*sdPoint, YourOrderComment, MagicNumber, 0, Red);
			return;
		}
	}
}

bool IsOpenOrder()
{
	for (k = OrdersTotal() - 1; k >= 0; k--)
	{
		if (OrderSelect(k, SELECT_BY_POS, MODE_TRADES))
		{
			if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) { return true; }
		}
	}

	return false;
}

bool BuySignal()
{
	bool isBuy = false;
	datetime     TradeStart, TradeEnd;
	double       SymSpread, RangeHigh, RangeLow, Lot, SL;

	TradeStart = StringToTime(TimeToString(TimeCurrent(), TIME_DATE)) + LondonOpen;
	TradeEnd   = TradeStart + 18000;   //--- 18000 sec = 5 hours

	//--- Identify trading range
	ShiftLondon = int((iTime(NULL, PERIOD_M15, 0) - TradeStart) / 900);   //--- 900 sec = 15 min
	RangeHigh = iHigh(NULL, PERIOD_M15, iHighest(NULL, PERIOD_M15, MODE_HIGH, 6, ShiftLondon + 1));
	RangeLow = iLow(NULL, PERIOD_M15, iLowest(NULL, PERIOD_M15, MODE_LOW, 6, ShiftLondon + 1));

	if ((RangeHigh - RangeLow) / Point > MinRange && (RangeHigh - RangeLow) / Point < MaxRange)
	{
		Pos = PosLongShort;
		if (Bid > RangeHigh + RangeSpread && Bid < RangeHigh + RangeSpread * 3 && (Pos == PosLongOnly || Pos == PosLongShort))
		{
			if (iHigh(NULL, PERIOD_M15, 1) > RangeHigh + RangeSpread) { return; }
			SL = Ask - StopLoss * Point;
			if (iLow(NULL, PERIOD_M1, 0) <= SL) { return; }
			
			Lot = CalculateVolume(OP_BUY, SL);

			if (!CheckVolume(Lot))
			{
				Print(Symbol(), " - ", ErrMsg);
			}
			else if (AccountFreeMarginCheck(Symbol(), OP_BUY, Lot) <= 0.0 || _LastError == ERR_NOT_ENOUGH_MONEY)
			{
				Print(Symbol(), " - ", ErrorDescription(GetLastError()));
			}
			else if (OrderSend(Symbol(), OP_BUY, Lot, Ask, Slippage, SL, TP, "VFX", MagicNumber, 0, clrBlue) == -1)
			{
				Print(Symbol(), " - ", ErrorDescription(GetLastError()));
			}
		}
		else if (Bid < RangeLow - RangeSpread && Bid > RangeLow - RangeSpread * 3 && (Pos == PosShortOnly || Pos == PosLongShort))
		{
			if (iLow(NULL, PERIOD_M15, 1) < RangeLow - RangeSpread) { return; }
			SL = Bid + StopLoss * Point;
			if (iHigh(NULL, PERIOD_M1, 0) + SymSpread >= SL) { return; }
			TP = Bid - TakeProfit * Point;
			Lot = CalculateVolume(OP_SELL, SL);

			if (!CheckVolume(Lot))
			{
				Print(Symbol(), " - ", ErrMsg);
			}
			else if (AccountFreeMarginCheck(Symbol(), OP_SELL, Lot) <= 0.0 || _LastError == ERR_NOT_ENOUGH_MONEY)
			{
				Print(Symbol(), " - ", ErrorDescription(GetLastError()));
			}
			else if (OrderSend(Symbol(), OP_SELL, Lot, Bid, Slippage, SL, TP, "VFX", MagicNumber, 0, clrRed) == -1)
			{
				Print(Symbol(), " - ", ErrorDescription(GetLastError()));
			}
		}
	}

	return isBuy;
}

bool SellSignal()
{
	bool isSell = false;

	return isSell;
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

bool CheckVolume(double Lot)
{
	//--- Minimal allowed volume for trade operations
	double MinVolume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
	if (Lot < MinVolume)
	{
		ErrMsg = StringConcatenate("Volume less than the minimum allowed. The minimum volume is ", MinVolume, ".");
		return(false);
	}

	//--- Maximal allowed volume of trade operations
	double MaxVolume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
	if (Lot > MaxVolume)
	{
		ErrMsg = StringConcatenate("Volume greater than the maximum allowed. The maximum volume is ", MaxVolume, ".");
		return(false);
	}

	//--- Get minimal step of volume changing
	double VolumeStep = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);

	int Ratio = (int)MathRound(Lot / VolumeStep);
	if (MathAbs(Ratio * VolumeStep - Lot) > 0.0000001)
	{
		ErrMsg = StringConcatenate("The volume is not multiple of the minimum gradation ", VolumeStep,
			". Volume closest to the valid ", Ratio * VolumeStep, ".");
		return(false);
	}

	//--- Correct volume value
	return(true);
}


// the end.