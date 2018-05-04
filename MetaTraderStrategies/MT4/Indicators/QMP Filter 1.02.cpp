//+------------------------------------------------------------------+
//|                                                   QMP Filter.mq4 |
//|                                     contactchristinali@gmail.com |
//|                               http//www.wix.com/wiseea/wise-ea#! | 
//+------------------------------------------------------------------+
#property copyright     "Programmed by Christina Li, Wise-EA MetaTrader Programming"
#property link          "www.wix.com/wiseea/wise-ea#! "
#property version       "1.02"
#property description   "Copyright Jim Brown"
#property description   "Updated on 05/23/2014"
#property description   "Initial version, an indicator signals based on MACD Platinum & QQE on multiple time frames"
//+------------------------------------------------------------------+
//| Setup & Include                                                  |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_color1  clrLimeGreen
#property indicator_color2  clrRed
#property indicator_color3  clrRoyalBlue
#property indicator_color4  clrOrangeRed
//+------------------------------------------------------------------+
//| Input parameters                                                 |
//+------------------------------------------------------------------+
input int    HigherTimeFrame          = 0;
input int    Fast_Platinum            = 12;
input int    Slow_Platinum            = 26;
input int    Smooth_Platinum          = 9;
input int    SF                       = 1;
input int    RSI_Period               = 8;
input int    WP                       = 3;
input string AlertSound               = "alert.wav";
input bool   AlertEnabled             = true;

//+------------------------------------------------------------------+
//| Global variabels                                                 |
//+------------------------------------------------------------------+
double up[], dn[], uph[], dnh[];
string trend, trendh;
static datetime BarTime;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit(void)
  {
//----
   SetIndexStyle(0,DRAW_ARROW,STYLE_SOLID,1);
   SetIndexBuffer(0,up);
   SetIndexArrow(0,108);
   SetIndexStyle(1,DRAW_ARROW,STYLE_SOLID,1);
   SetIndexBuffer(1,dn);
   SetIndexArrow(1,108);  
//----   
   SetIndexStyle(2,DRAW_ARROW,STYLE_SOLID,2);
   SetIndexBuffer(2,uph);
   SetIndexArrow(2,233);
   SetIndexStyle(3,DRAW_ARROW,STYLE_SOLID,2);
   SetIndexBuffer(3,dnh);
   SetIndexArrow(3,234); 
//---
   trend="X";
   trendh="X";
   BarTime=0;
  }
//+------------------------------------------------------------------+
//| Custom indicator calculate function                              |
//+------------------------------------------------------------------+
int OnCalculate (const int rates_total,
                 const int prev_calculated,
                 const datetime& time[],
                 const double& open[],
                 const double& high[],
                 const double& low[],
                 const double& close[],
                 const long& tick_volume[],
                 const long& volume[],
                 const int& spread[])
  {
//----
   int i,limit;
   limit=rates_total-prev_calculated;
   if(prev_calculated>0) limit++;
//----
   for (i=limit; i>=0; i--) 
     {
      double atr=iATR(NULL,0,100,i);
      //----
      if (i>0 && trend!="Up" && dir(0,i)=="Up")
        {
         trend="Up";
         up[i]=Low[i]-atr/2;
         if (AlertEnabled && isNewBar(Time[i])) {
             Comment("QMP Filter Long Signal");
             Print("QMP Filter Long Signal");
             PlaySound(AlertSound);
         }
        }
      else if (i>0 && trend!="Dn" && dir(0,i)=="Dn") 
        {
         trend="Dn";
         dn[i]=High[i]+atr/2;
         if (AlertEnabled && isNewBar(Time[i])) {
             Comment("QMP Filter Short Signal");
             Print("QMP Filter Short Signal");
             PlaySound(AlertSound);
         }
        }
      //----
      if (HigherTimeFrame>0 && HigherTimeFrame>Period())
        {
         if (BarTime==0) BarTime=iTime(NULL,HigherTimeFrame,iBarShift(NULL,HigherTimeFrame,Time[i]));
         if (isNewBar(Time[i]))
           {
            int shift=iBarShift(NULL,HigherTimeFrame,Time[i])+1;
            if (trendh!="Up" && dir(HigherTimeFrame,shift)=="Up")
              {
               trendh="Up";
               uph[i+1]=Low[i+1]-1.5*atr;
              } 
            else if (trendh!="Dn" && dir(HigherTimeFrame,shift)=="Dn")
              {
               trendh="Dn";
               dnh[i+1]=High[i+1]+1.5*atr;
              }
           } 
        }
     }
//----
   return(rates_total);
  }
//******************************************************************************************************
//******************************************************************************************************
//+------------------------------------------------------------------+
//| FUNCTIONS: New bar                                               |
//+------------------------------------------------------------------+
bool isNewBar(datetime curtime) 
   {
//---- 
   bool res=false; 
   if (iBarShift(NULL,HigherTimeFrame,BarTime)!=iBarShift(NULL,HigherTimeFrame,curtime)) 
     {
      BarTime=curtime;
      res=true;
     } 
//----    
   return(res);   
  }
//+------------------------------------------------------------------+
//| FUNCTIONS: Current time frame condition check                    |
//+------------------------------------------------------------------+
string dir(int tf, int x) 
  {
//----
   string ctrend="X";
   double blue=iCustom(Symbol(),tf,"MACD_Platinum",Fast_Platinum,Slow_Platinum,Smooth_Platinum,true,false,false,"",false,false,false,4,x);
   double orange=iCustom(Symbol(),tf,"MACD_Platinum",Fast_Platinum,Slow_Platinum,Smooth_Platinum,true,false,false,"",false,false,false,5,x);
   double qqe1=iCustom(Symbol(),tf,"QQE Adv",SF,RSI_Period,WP,0,x);
   double qqe2=iCustom(Symbol(),tf,"QQE Adv",SF,RSI_Period,WP,1,x);
//----
   if (blue>=orange && qqe1>=qqe2) ctrend="Up";
   else if (blue<orange && qqe1<qqe2) ctrend="Dn";
//----
   return (ctrend);
  }