#property copyright "Copyright 2018, Robert Chambers"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 2
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
double _mediumTermTrendData[];
double _shortTermTrendData[];
int _platinumHandle;
int _qmpFilterHandle;
int _longTermTrendHandle;
int _mediumTermTrendHandle;
int _shortTermTrendHandle;


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
    //PlotIndexSetInteger(0,PLOT_ARROW_SHIFT,10);
    //PlotIndexSetInteger(1,PLOT_ARROW_SHIFT,-20);
    //PlotIndexSetInteger(2,PLOT_ARROW,159);
    //PlotIndexSetInteger(3,PLOT_ARROW,159);   

    //string ShortName=MQLInfoString(MQL_PROGRAM_NAME)+"-"+IntegerToString(ID);
    //IndicatorSetString(INDICATOR_SHORTNAME,ShortName);
    //GlobalVariableSet(ShortName+"_Shift",1);   
    //GlobalVariableSet(ShortName+"_UseTarget",1);   

    IndicatorSetString(INDICATOR_SHORTNAME, "Specific QMP Filter");

    ArraySetAsSeries(Label1Buffer, true);
    ArraySetAsSeries(Label2Buffer, true);

    ArraySetAsSeries(_qmpFilterUpData, true);
    ArraySetAsSeries(_qmpFilterDownData, true);
    ArraySetAsSeries(_longTermTrendData, true);
    ArraySetAsSeries(_mediumTermTrendData, true);
    ArraySetAsSeries(_shortTermTrendData, true);

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

    _mediumTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 100, 0, MODE_EMA, PRICE_CLOSE);
    if (_mediumTermTrendHandle == INVALID_HANDLE) {
        Print("Error creating medium term MA indicator");
        return INIT_FAILED;
    }

    _shortTermTrendHandle = iMA(_Symbol, PERIOD_CURRENT, 50, 0, MODE_EMA, PRICE_CLOSE);
    if (_shortTermTrendHandle == INVALID_HANDLE) {
        Print("Error creating short term MA indicator");
        return INIT_FAILED;
    }
     
//---
   return INIT_SUCCEEDED;
}
  
void OnDeinit(const int r){
   //ObjectsDeleteAll(0,MQLInfoString(MQL_PROGRAM_NAME));
    
    ReleaseIndicator(_qmpFilterHandle);
    ReleaseIndicator(_platinumHandle);
    ReleaseIndicator(_longTermTrendHandle);
    ReleaseIndicator(_mediumTermTrendHandle);
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

   //int limit = rates_total - prev_calculated;
   //if (limit > 1)
   //{
   //    limit = rates_total - 2;
   //    ArrayInitialize(Label1Buffer, EMPTY_VALUE);
   //    ArrayInitialize(Label2Buffer, EMPTY_VALUE);
   //}

   //if (prev_calculated == 0) {   
   //   Label1Buffer[0] = -1;
   //}
   //else {
   //   start = prev_calculated - 1;
   //}
   
   int count = (limit == 0 ? 1 : rates_total);
   /* Print("count = ", count, " and rates_total = ", rates_total, " and limit = ", limit);   
   count = 100575 and rates_total = 100575 and limit = 100565
   count = 1 and rates_total = 100575 and limit = 0
   */


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

   copied = CopyBuffer(_mediumTermTrendHandle, 0, 0, count, _mediumTermTrendData);
   if (copied == -1) {
       Print("Error copying medium term trend data.");
       return 0;
   }

   copied = CopyBuffer(_shortTermTrendHandle, 0, 0, count, _shortTermTrendData);
   if (copied == -1) {
       Print("Error copying short term trend data.");
       return 0;
   }

   ArraySetAsSeries(close, true);
   ArraySetAsSeries(high, true);
   ArraySetAsSeries(low, true);

   for (int i = limit; i >= 0 && !IsStopped(); i--)
   //for (int i = 0; i < limit && !IsStopped(); i++)
   {
       // Bullish
       Label1Buffer[i] = 0.0;
        if (_qmpFilterUpData[i] && _qmpFilterUpData[i] != 0.0) {
            if (i < ArraySize(close) && i < ArraySize(low) && close[i] >= _longTermTrendData[i] && low[i] < _shortTermTrendData[i]) {
                Label1Buffer[i] = _qmpFilterUpData[i];
            }
        }

        // Bearish
        Label2Buffer[i] = 0.0;
        if (_qmpFilterDownData[i] && _qmpFilterDownData[i] != 0.0) {
            if (i < ArraySize(close) && i < ArraySize(high) && close[i] <= _longTermTrendData[i] && high[i] >= _shortTermTrendData[i]) {
                Label2Buffer[i] = _qmpFilterDownData[i];
            }
        }
   }

   //--- return value of prev_calculated for next call
   return(rates_total);

   /*
   for (int i = limit; i >= 0 && !IsStopped(); i--) {
      int count = CopyBuffer(_qmpFilterHandle, 0, 0, 2, _qmpFilterUpData);

      count = CopyBuffer(_qmpFilterHandle, 1, 0, 2, _qmpFilterDownData);
      if (count == -1) {
          Print("Error copying QMP Filter data for down buffer.");
          return 0;
      }

      count = CopyBuffer(_longTermTrendHandle, 0, 0, 2, _longTermTrendData);
      if (count == -1) {
          Print("Error copying long term trend data.");
          return 0;
      }

      count = CopyBuffer(_mediumTermTrendHandle, 0, 0, 2, _mediumTermTrendData);
      if (count == -1) {
          Print("Error copying medium term trend data.");
          return 0;
      }

      count = CopyBuffer(_shortTermTrendHandle, 0, 0, 2, _shortTermTrendData);
      if (count == -1) {
          Print("Error copying short term trend data.");
          return 0;
      }


      if (_qmpFilterUpData[1] && _qmpFilterUpData[1] != 0.0) {
          if (i < ArraySize(low)) {
              Label1Buffer[0] = low[i];
          }
          else if (i < ArraySize(close)) {
              Label1Buffer[0] = close[i];
          }

          //if (close[i] >= _longTermTrendData[1] && close[i] < _shortTermTrendData[1] && close[i] < _mediumTermTrendData[1]) {
          //    Print("Buy signal!");
          //    Label1Buffer[i] = low[i];
          //}
      }
      else if (_qmpFilterDownData[1] && _qmpFilterDownData[1] != 0.0) {
          if (i < ArraySize(high)) {
              Label2Buffer[0] = high[i];
          }

          //if (close[i] <= _longTermTrendData[1] && close[i] > _shortTermTrendData[1] && close[i] > _mediumTermTrendData[1]) {           
          //    Label2Buffer[i] = high[i];
          //}
      }
   }
   */
   
   //return rates_total;
}

void ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}