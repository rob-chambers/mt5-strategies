#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property indicator_separate_window
#property indicator_buffers 2
#property indicator_plots   2
//--- plot Label1
#property indicator_label1  "Balance"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrRed
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot Label2
#property indicator_label2  "Equity"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrGreen
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1

input int                  ID             =  1;
input double               StopLoss_K     =  1;
input bool                 FixedSLTP      =  false;
input int                  StopLoss       =  500;
input int                  TakeProfit     =  500;

//--- indicator buffers
double         BalanceBuffer[];
double         EquityBuffer[];

string IndName;

struct SPos{
   int dir;
   double price;
   double sl;
   double tp;
   datetime time; 
};
SPos Pos[];
int PosCnt;

datetime LastPosTime;

int Closed;
datetime CloseTime;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
  
   IndName=MQLInfoString(MQL_PROGRAM_NAME);
  
//--- indicator buffers mapping
   SetIndexBuffer(0,BalanceBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,EquityBuffer,INDICATOR_DATA);
   
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
   Comment("");
//---
   return(INIT_SUCCEEDED);
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
                const int &spread[]){
     
   
   string name;
   static int last_handle=-1;
   static int shift=0;
   static int use_target=0;
   int handle=-1;     
   int start=2;  
                 
   int it=ChartIndicatorsTotal(0,0);


   for(int i=0;i<it;i++){
      name=ChartIndicatorName(0,0,i);
      int p=StringFindRev(name,"-");
      
      if(p!=-1){
         if(StringSubstr(name,p+1,StringLen(name)-p-1)==IntegerToString(ID)){
            handle=ChartIndicatorGet(0,0,name);
         }
      }
   }
   
   if(handle!=last_handle){
      if(handle==-1){
         IndicatorSetString(INDICATOR_SHORTNAME,IndName);
         ChartRedraw(0);
         return(0);
      }
      int bc=BarsCalculated(handle);
      if(bc<=0)return(0);
      double sh[1];
      if(CopyBuffer(handle,0,rates_total-1,1,sh)==-1){
         return(0);
      }
      shift=((int)sh[0])/10;
      use_target=((int)sh[0])%10;
      last_handle=handle;
      IndicatorSetString(INDICATOR_SHORTNAME,name);
      ChartRedraw(0);
   }
   else if(prev_calculated!=0){
      start=prev_calculated-1;
   }
   
   if(start==2){
      PosCnt=0;
      BalanceBuffer[1]=0;
      EquityBuffer[1]=0;
      LastPosTime=0;
      Closed=0;
      CloseTime=0;      
   }
   
   for(int i=start;i<rates_total;i++){
   
      BalanceBuffer[i]=BalanceBuffer[i-1];
   
      if(CloseTime!=time[i]){ 
         Closed=0;
         CloseTime=time[i];          
      }
   
      double buy[1],sell[1],buy_target[1],sell_target[1],enter[1];
      int ind=rates_total-i-1+shift;
      if(CopyBuffer(last_handle,0,ind,1,buy)==-1 || 
         CopyBuffer(last_handle,1,ind,1,sell)==-1 ||
         CopyBuffer(last_handle,2,ind,1,buy_target)==-1 || 
         CopyBuffer(last_handle,3,ind,1,sell_target)==-1        
      ){
         return(0);
      }
    
      if(shift==0){
         if(CopyBuffer(last_handle,4,ind,1,enter)==-1){
            return(0);
         } 
      }
      else{
         enter[0]=open[i];
      }
   
      if(buy[0]!=EMPTY_VALUE){
         AddPos(1,enter[0],buy_target[0],spread[i],time[i],use_target);      
      }
      if(sell[0]!=EMPTY_VALUE){
         AddPos(-1,enter[0],sell_target[0],spread[i],time[i],use_target);       
      }
   
      CheckClose(i,high,low,close,spread);
   
      BalanceBuffer[i]+=Closed;
      
      EquityBuffer[i]=BalanceBuffer[i]+SolveEquity(i,close,spread);
   
   }

   return(rates_total);
}
  
int SolveEquity(int i,const double & close[],const int & spread[]){
   int rv=0;
   for(int j=PosCnt-1;j>=0;j--){
      if(Pos[j].dir==1){
         rv+=(int)((close[i]-Pos[j].price)/Point());
      }
      else{
         rv+=(int)((Pos[j].price+Point()*spread[i]-close[i])/Point());         
      }
   }
   return(rv);
}  

void CheckClose(int i,const double & high[],const double & low[],const double & close[],const int & spread[]){
   for(int j=PosCnt-1;j>=0;j--){
      bool closed=false;
      if(Pos[j].dir==1){
         if(low[i]<=Pos[j].sl){
            Closed+=(int)((Pos[j].sl-Pos[j].price)/Point());
            closed=true;
         }
         else if(high[i]>=Pos[j].tp){
            Closed+=(int)((Pos[j].tp-Pos[j].price)/Point());    
            closed=true;
         }
      }
      else{
         if(high[i]+Point()*spread[i]>=Pos[j].sl){
            Closed+=(int)((Pos[j].price-Pos[j].sl)/Point());
            closed=true;
         }
         else if(low[i]+Point()*spread[i]<=Pos[j].tp){
            Closed+=(int)((Pos[j].price-Pos[j].tp)/Point());              
            closed=true;
         }         
      }
      if(closed){ 
         int ccnt=PosCnt-j-1;
         if(ccnt>0){
            ArrayCopy(Pos,Pos,j,j+1,ccnt);
         }
         PosCnt--;
      }
   }
}
 
void AddPos(int dir, double price,double target,int spread,datetime time,bool use_target){
   if(time<=LastPosTime){
      return;
   }
   
   if(PosCnt>=ArraySize(Pos)){
      ArrayResize(Pos,ArraySize(Pos)+32);
   }
   
   Pos[PosCnt].dir=dir;
   Pos[PosCnt].time=time;
   if(dir==1){
      Pos[PosCnt].price=price+Point()*spread;  
   }
   else{
      Pos[PosCnt].price=price;  
   }

   if(use_target && !FixedSLTP){ 
      if(dir==1){
         Pos[PosCnt].tp=target;
         Pos[PosCnt].sl=NormalizeDouble(Pos[PosCnt].price-StopLoss_K*(Pos[PosCnt].tp-Pos[PosCnt].price),Digits());
      }
      else{
         Pos[PosCnt].tp=target+Point()*spread;
         Pos[PosCnt].sl=NormalizeDouble(Pos[PosCnt].price+StopLoss_K*(Pos[PosCnt].price-Pos[PosCnt].tp),Digits());
      }   
   }
   else{
      if(dir==1){
         Pos[PosCnt].tp=Pos[PosCnt].price+Point()*TakeProfit;
         Pos[PosCnt].sl=Pos[PosCnt].price-Point()*StopLoss;
      }
      else{
         Pos[PosCnt].tp=Pos[PosCnt].price-Point()*TakeProfit;
         Pos[PosCnt].sl=Pos[PosCnt].price+Point()*StopLoss;
      }     
   }
   PosCnt++;
}  
  
int StringFindRev(string str,string find){
   int rv;
   int start=-1;
   do{
      rv=start;
      start=StringFind(str,find,start+1);
   }
   while(start!=-1);
   return(rv);
}  
