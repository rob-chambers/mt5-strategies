#property copyright "Copyright 2019, Robert Chambers"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 3
#property indicator_plots   2
//--- plot Label1
#property indicator_label1  "Buy"
#property indicator_type1   DRAW_ARROW
#property indicator_color1  clrDodgerBlue
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot Label2
#property indicator_label2  "Sell"
#property indicator_type2   DRAW_ARROW
#property indicator_color2  clrRed
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1

double Label1Buffer[];
double Label2Buffer[];
double _qmpFilterUpData[];
double _qmpFilterDownData[];
double _longTermTrendData[];
double _shortTermTrendData[];
double _platinumUpCrossData[];
double _platinumDownCrossData[];
int _platinumHandle;
int _qmpFilterHandle;
int _longTermTrendHandle;
int _shortTermTrendHandle;
double _upCrossRecentValue, _upCrossPriorValue, _downCrossRecentValue, _downCrossPriorValue;
int _upCrossRecentIndex, _upCrossPriorIndex, _downCrossRecentIndex, _downCrossPriorIndex;
MqlRates _prices[];
int _inpFastPlatinum = 12, _inpSlowPlatinum = 26, _inpSmoothPlatinum = 9;
double _upCrossRecentPrice;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit(){
    //--- indicator buffers mapping
    SetIndexBuffer(0, Label1Buffer, INDICATOR_DATA);
    SetIndexBuffer(1, Label2Buffer, INDICATOR_DATA);

    //--- setting a code from the Wingdings charset as the property of PLOT_ARROW
    PlotIndexSetInteger(0, PLOT_ARROW, 233);
    PlotIndexSetInteger(1, PLOT_ARROW, 234);

    IndicatorSetString(INDICATOR_SHORTNAME, "Divergence QMP Filter");

    ArraySetAsSeries(Label1Buffer, true);
    ArraySetAsSeries(Label2Buffer, true);

    ArraySetAsSeries(_qmpFilterUpData, true);
    ArraySetAsSeries(_qmpFilterDownData, true);
	ArraySetAsSeries(_platinumUpCrossData, true);
	ArraySetAsSeries(_platinumDownCrossData, true);
    ArraySetAsSeries(_longTermTrendData, true);
    ArraySetAsSeries(_shortTermTrendData, true);
	ArraySetAsSeries(_prices, true);

    _platinumHandle = iCustom(_Symbol, PERIOD_CURRENT, "MACD_Platinum", 12, 26, 9, true, true, false, false);
    if (_platinumHandle == INVALID_HANDLE) {
        Print("Error creating MACD Platinum indicator");
        return INIT_FAILED;
    }

    _qmpFilterHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", PERIOD_CURRENT, 12, 26, 9, true, 1, 8, 3, false, false);
    if (_qmpFilterHandle == INVALID_HANDLE) {
        Print("Error creating QMP Filter indicator");
        return INIT_FAILED;
    }

    _longTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 240, 0, MODE_LWMA, PRICE_CLOSE);
    if (_longTermTrendHandle == INVALID_HANDLE) {
        Print("Error creating long term MA indicator");
        return INIT_FAILED;
    }

    _shortTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 50, 0, MODE_EMA, PRICE_CLOSE);
    if (_shortTermTrendHandle == INVALID_HANDLE) {
        Print("Error creating short term MA indicator");
        return INIT_FAILED;
    }
     
   return INIT_SUCCEEDED;
}
  
void OnDeinit(const int r){
    
    ReleaseIndicator(_qmpFilterHandle);
    ReleaseIndicator(_platinumHandle);
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_shortTermTrendHandle);

   ChartRedraw();
}  
  
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {  
   if (Bars(_Symbol, _Period) < rates_total) return prev_calculated;
 
   int limit = rates_total - prev_calculated;
   if (limit > 1)
   {
       limit = rates_total - 10;
       ArrayInitialize(Label1Buffer, EMPTY_VALUE);
       ArrayInitialize(Label2Buffer, EMPTY_VALUE);
   }
 
   int count = (limit == 0 ? 1 : rates_total);

   int copied = CopyBuffer(_qmpFilterHandle, 0, 0, count, _qmpFilterUpData);
   if (copied == -1) {
       Print("Error copying QMP Filter data for up buffer.");
       return 0;
   }

   copied = CopyBuffer(_qmpFilterHandle, 1, 0, count, _qmpFilterDownData);
   if (copied == -1) {
       Print("Error copying QMP Filter data for down buffer.");
       return 0;
   }

   copied = CopyBuffer(_longTermTrendHandle, 0, 0, count, _longTermTrendData);
   if (copied == -1) {
       Print("Error copying long term trend data.");
       return 0;
   }

   copied = CopyBuffer(_shortTermTrendHandle, 0, 0, count, _shortTermTrendData);
   if (copied == -1) {
       Print("Error copying short term trend data.");
       return 0;
   }

   copied = CopyBuffer(_platinumHandle, 2, 0, count, _platinumUpCrossData);
    if (copied == -1) {
        Print("Error copying platinum up cross data.");
        return 0;
    }

    copied = CopyBuffer(_platinumHandle, 3, 0, count, _platinumDownCrossData);
    if (copied == -1) {
        Print("Error copying platinum down cross data.");
        return 0;
    }

   ArraySetAsSeries(close, true);
   ArraySetAsSeries(high, true);
   ArraySetAsSeries(low, true);

   /*
   	int numberOfPriceDataPoints = CopyRates(_Symbol, 0, 0, 40, _prices);
    if (numberOfPriceDataPoints == -1) {
        Print("Error copying rates during processing.");
        return 0;
    }
	*/

   for (int i = limit; i >= 0 && !IsStopped(); i--)
   {
       // Bullish
       Label1Buffer[i] = 0.0;

        if (_qmpFilterUpData[i] && _qmpFilterUpData[i] != 0.0) {
            if (i < ArraySize(close) && close[i] >= _longTermTrendData[i]) {				
				if (_platinumUpCrossData[i] < -0.004) {
					Label1Buffer[i] = _qmpFilterUpData[i];
				}
            }
        }

        // Bearish
        Label2Buffer[i] = 0.0;
        if (_qmpFilterDownData[i] && _qmpFilterDownData[i] != 0.0) {
            if (i < ArraySize(close) && close[i] <= _longTermTrendData[i]) {
				if (_platinumDownCrossData[i] > 0.004) {
					Label2Buffer[i] = _qmpFilterDownData[i];
				}
            }
        }
   }

   //--- return value of prev_calculated for next call
   return(rates_total);   
}

void ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}