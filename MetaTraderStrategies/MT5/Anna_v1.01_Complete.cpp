//+------------------------------------------------------------------+
//|                                                    Anna 1.01.mq5 
//|                                       Copyright 2016, Lucas Liew
//|                                https://blackalgotechnologies.com/
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Lucas Liew"
#property link      "https://blackalgotechnologies.com/"
#property version   "1.01"

/*

Learning Objective: To get data on Open, High, Low and Close prices.

Anna 1.01's Rules:

Entries:
- Enter a long trade when SMA(10) crosses SMA(40) from bottom if High[1] - Low[1] is greater than High[2] - Low[2]
- Enter a short trade when SMA(10) crosses SMA(40) from top if High[1] - Low[1] is greater than High[2] - Low[2]

Exits:
- Exit the long trade when SMA(10) crosses SMA(40) from top
- Exit the short trade when SMA(10) crosses SMA(40) from bottom
- 30 pips hard stop (30pips from initial entry price)
   
Position Sizing
   - Enter 1 lot at a time
   - Maximum number of open positions = 1
   
Notes (for the more advanced MQL5 traders):
   - We are using Hedging accounts. Thus, we are doing order-centric trade management.
      
*/

#include <Trade\Trade.mqh> // Get code from other places

//--- Input Variables (Accessible from MetaTrader 5)

input double   lot = 1;
input int      shortSmaPeriods = 10;
input int      longSmaPeriods = 40;
input int      slippage = 3;
input bool     useStopLoss = true;
input double   stopLossPips = 30;
input bool     useTakeProfit = true;
input double   takeProfitPips = 60;

//--- Service Variables (Only accessible from the MetaEditor)

CTrade myTradingControlPanel;
MqlRates PriceDataTable[]; 
double shortSmaData[], longSmaData[]; // You can declare multiple variables of the same data type in the same line.
int numberOfPriceDataPoints, numberOfShortSmaData, numberOfLongSmaData; 
int shortSmaControlPanel, longSmaControlPanel;
int P;
double currentBid, currentAsk;
double stopLossPipsFinal, takeProfitPipsFinal, stopLevelPips;
double stopLossLevel, takeProfitLevel;
double shortSma1, shortSma2, longSma1, longSma2;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ArraySetAsSeries(PriceDataTable,true); // Setting up table/array for time series data
   ArraySetAsSeries(shortSmaData,true);   // Setting up table/array for time series data
   ArraySetAsSeries(longSmaData,true);    // Setting up table/array for time series data
      
   shortSmaControlPanel = iMA(_Symbol, _Period, shortSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for short SMA
   longSmaControlPanel = iMA(_Symbol, _Period, longSmaPeriods, 0, MODE_SMA, PRICE_CLOSE); // Getting the Control Panel/Handle for long SMA
   
   if(_Digits == 5 || _Digits == 3 || _Digits == 1) P = 10;else P = 1; // To account for 5 digit brokers
   
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   IndicatorRelease(shortSmaControlPanel);
   IndicatorRelease(longSmaControlPanel);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   // -------------------- Collect most current data --------------------
   
   currentBid = SymbolInfoDouble(_Symbol,SYMBOL_BID); // Get latest Bid Price
   currentAsk = SymbolInfoDouble(_Symbol,SYMBOL_ASK); // Get latest Ask Price
   
   numberOfPriceDataPoints = CopyRates(_Symbol,0,0,10,PriceDataTable); // Collects data from shift 0 to shift 9
   numberOfShortSmaData = CopyBuffer(shortSmaControlPanel, 0, 0, 3, shortSmaData); // Collect most current SMA(10) Data and store it in the datatable/array shortSmaData[]
   numberOfLongSmaData = CopyBuffer(longSmaControlPanel, 0, 0, 3, longSmaData); // Collect most current SMA(40) Data and store it in the datatable/array longSmaData[]
   
   shortSma1 = shortSmaData[1];
   shortSma2 = shortSmaData[2]; 
   longSma1 = longSmaData[1]; 
   longSma2 = longSmaData[2]; 
   
   // -------------------- Technical Requirements --------------------
   
   // Explanation: Stop Loss and Take Profit levels can't be too close to our order execution price. We will talk about this again in a later lecture.
   // Resources for learning more: https://book.mql4.com/trading/orders (ctrl-f search "stoplevel"); https://book.mql4.com/appendix/limits
   
   stopLevelPips = (double) (SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) + SymbolInfoInteger(_Symbol, SYMBOL_SPREAD)) / P; // Defining minimum StopLevel

   if (stopLossPips < stopLevelPips) 
      {
      stopLossPipsFinal = stopLevelPips;
      } 
   else
      {
      stopLossPipsFinal = stopLossPips;
      } 
      
   if (takeProfitPips < stopLevelPips) 
      {
      takeProfitPipsFinal = stopLevelPips;
      }
   else
      {
      takeProfitPipsFinal = takeProfitPips;
      }
      
  // -------------------- EXITS --------------------
   
   if(PositionSelect(_Symbol) == true) // We have an open position
      { 
      
      // --- Exit Rules (Long Trades) ---
      
      /*
      Exits:
      - Exit the long trade when SMA(10) crosses SMA(40) from top
      - Exit the short trade when SMA(10) crosses SMA(40) from bottom
      */
      
      // TDL 3: Enter exit rule for long trades
      
      // --------------------------------------------------------- //
      
      if(shortSma2 > longSma2 && shortSma1 <= longSma1) // Rule to exit long trades
         
      // --------------------------------------------------------- //
         
         {
         if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) // If it is Buy position
            { 
            
            myTradingControlPanel.PositionClose(_Symbol); // Closes position related to this symbol
            
            if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
               {
               Print("Exit rules: A close order has been successfully placed with Ticket#: ",myTradingControlPanel.ResultOrder());
               }
            else
               {
               Print("Exit rules: The close order request could not be completed.Error: ",GetLastError());
               ResetLastError();
               return;
               }
               
            }
         }
      
      // TDL 4: Enter exit rule for short trades
      
      // --------------------------------------------------------- //
      
      if(shortSma2 < longSma2 && shortSma1 >= longSma1) // Rule to exit short trades
         
      // --------------------------------------------------------- //  
       
         {
         if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL) // If it is Sell position
            { 
            
            myTradingControlPanel.PositionClose(_Symbol); // Closes position related to this symbol
            
            if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
               {
               Print("Exit rules: A close order has been successfully placed with Ticket#: ", myTradingControlPanel.ResultOrder());
               }
            else
               {
               Print("Exit rules: The close order request could not be completed. Error: ", GetLastError());
               ResetLastError();
               return;
               }
            }
         }
      
      }
   
   // -------------------- ENTRIES --------------------  
         
   if(PositionSelect(_Symbol) == false) // We have no open position
      { 
      
      // --- Entry Rules (Long Trades) ---
      
      /*
      Entries:
      - Enter a long trade when SMA(10) crosses SMA(40) from bottom
      - Enter a short trade when SMA(10) crosses SMA(40) from top
      */
      
      // TDL 1: Enter entry rule for long trades
      
      // --------------------------------------------------------- //
      
      if(PriceDataTable[1].high - PriceDataTable[1].low > PriceDataTable[2].high - PriceDataTable[2].low && 
      shortSma2 < longSma2 && shortSma1 >= longSma1) // Rule to enter long trades
      // --------------------------------------------------------- //
     
         {   
         
         if (useStopLoss) stopLossLevel = currentAsk - stopLossPipsFinal * _Point * P; else stopLossLevel = 0.0;
         if (useTakeProfit) takeProfitLevel = currentAsk + takeProfitPipsFinal * _Point * P; else takeProfitLevel = 0.0;
        
         myTradingControlPanel.PositionOpen(_Symbol, ORDER_TYPE_BUY, lot, currentAsk, stopLossLevel, takeProfitLevel, "Buy Trade. Magic Number #" + (string) myTradingControlPanel.RequestMagic()); // Open a Buy position
         
         if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
            {
            Print("Entry rules: A Buy order has been successfully placed with Ticket#: ", myTradingControlPanel.ResultOrder());
            }
         else
            {
            Print("Entry rules: The Buy order request could not be completed. Error: ", GetLastError());
            ResetLastError();
            return;
            }
           
         }
         
      // --- Entry Rules (Short Trades) ---
      
      /*
      Exit:
      - Exit the long trade when SMA(10) crosses SMA(40) from top
      - Exit the short trade when SMA(10) crosses SMA(40) from bottom
      */
      
      // TDL 2: Enter entry rule for short trades
      
      // --------------------------------------------------------- //
      
      else if(PriceDataTable[1].high - PriceDataTable[1].low > PriceDataTable[2].high - PriceDataTable[2].low && 
      shortSma2 > longSma2 && shortSma1 <= longSma1) // Rule to enter short trades
      
      // --------------------------------------------------------- //

         {   
         
         if (useStopLoss) stopLossLevel = currentBid + stopLossPipsFinal * _Point * P; else stopLossLevel = 0.0;
         if (useTakeProfit) takeProfitLevel = currentBid - takeProfitPipsFinal * _Point * P; else takeProfitLevel = 0.0;

         myTradingControlPanel.PositionOpen(_Symbol, ORDER_TYPE_SELL, lot, currentBid, stopLossLevel, takeProfitLevel, "Sell Trade. Magic Number #" + (string) myTradingControlPanel.RequestMagic()); // Open a Sell position
         
         if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
            {
            Print("Entry rules: A Sell order has been successfully placed with Ticket#: ", myTradingControlPanel.ResultOrder());
            }
         else
            {
            Print("Entry rules: The Sell order request could not be completed.Error: ", GetLastError());
            ResetLastError();
            return;
            }
         
         } 
      
      }
   
   } 
   
   
   
   
   
   
   
   
   
   
   
   



//+------------------------------------------------------------------+
