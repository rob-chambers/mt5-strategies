#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_plots   4
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
//--- plot Label3
#property indicator_label3  "BuyTarget"
#property indicator_type3   DRAW_ARROW
#property indicator_color3  clrDodgerBlue
#property indicator_style3  STYLE_SOLID
#property indicator_width3  1
//--- plot Label4
#property indicator_label4  "SellTarget"
#property indicator_type4   DRAW_ARROW
#property indicator_color4  clrRed
#property indicator_style4  STYLE_SOLID
#property indicator_width4  1

enum EAlerts{
   Alerts_off=0,
   Alerts_Bar0=1,
   Alerts_Bar1=2
};

input EAlerts              Alerts               =  Alerts_off;
input int                  ATRPeriod            =  50;
input double               K1                   =  3;
input double               MinOverlapping       =  0.4;
input int                  MinCount             =  5;
input bool                 FormTapered          =  true;
input double               FormTaperedK         =  0.05;
input bool                 FormRectangular      =  true;
input double               FormRectangularK     =  0.33;
input bool                 FormExpanding        =  true;
input double               FormExpandingK       =  0.05;
input bool                 InclineAlong         =  true;
input double               InclineAlongK        =  0.1;
input bool                 InclineHorizontal    =  true;
input double               InclineHorizontalK   =  0.1;
input bool                 InclineAgainst       =  true;
input double               InclineAgainstK      =  0.1;
input bool                 EnterAlong           =  true;
input bool                 EnterAgainst         =  true;
input int                  ID                   =  2;



//--- indicator buffers
double         Label1Buffer[];
double         Label2Buffer[];
double         Label3Buffer[];
double         Label4Buffer[];

int h;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit(){

   h=iATR(Symbol(),Period(),ATRPeriod);
   if(h==INVALID_HANDLE){
      Alert("Error load indicator");
      return(INIT_FAILED);
   }
   
//--- indicator buffers mapping
   SetIndexBuffer(0,Label1Buffer,INDICATOR_DATA);
   SetIndexBuffer(1,Label2Buffer,INDICATOR_DATA);
   SetIndexBuffer(2,Label3Buffer,INDICATOR_DATA);
   SetIndexBuffer(3,Label4Buffer,INDICATOR_DATA);   

//--- setting a code from the Wingdings charset as the property of PLOT_ARROW
   PlotIndexSetInteger(0,PLOT_ARROW,233);
   PlotIndexSetInteger(1,PLOT_ARROW,234);
   PlotIndexSetInteger(0,PLOT_ARROW_SHIFT,10);
   PlotIndexSetInteger(1,PLOT_ARROW_SHIFT,-10);
   PlotIndexSetInteger(2,PLOT_ARROW,159);
   PlotIndexSetInteger(3,PLOT_ARROW,159);   
   
   string ShortName=MQLInfoString(MQL_PROGRAM_NAME)+"-"+IntegerToString(ID);
   IndicatorSetString(INDICATOR_SHORTNAME,ShortName);
   GlobalVariableSet(ShortName+"_Shift",1);   
   GlobalVariableSet(ShortName+"_UseTarget",1);   
   
//---
   return(INIT_SUCCEEDED);
  }
  
void OnDeinit(const int r){
   ObjectsDeleteAll(0,MQLInfoString(MQL_PROGRAM_NAME));
   ChartRedraw();
}  
  
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+

struct SCurPre{
   int Whait;
   int Count;
   int Bar;
   void Init(){
      Whait=0;
      Count=0;
      Bar=0;
   }
};

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
  
   static datetime LastTime=0;   
   static SCurPre Cur;          
   static SCurPre Pre;

   int start=0;
   
   if(prev_calculated==0){
      
      start=1;      
      
      Cur.Init();
      Pre.Init();             
        
      LastTime=0;    

      Label1Buffer[0]=11;
        
   }
   else{
      start=prev_calculated-1;
   }
   
   for(int i=start;i<rates_total;i++){
      
      if(time[i]>LastTime){
         LastTime=time[i];
         Pre=Cur;              
      }
      else{
         Cur=Pre;
      }      
      
      Label1Buffer[i]=EMPTY_VALUE;
      Label2Buffer[i]=EMPTY_VALUE;      
      Label3Buffer[i]=EMPTY_VALUE;
      Label4Buffer[i]=EMPTY_VALUE;    
      
      double atr[1];
      if(CopyBuffer(h,0,rates_total-i-1,1,atr)==-1){
         return(0);
      }

      if(high[i]-low[i]>atr[0]*K1){
         if(close[i]>open[i]){
            Cur.Whait=1;
            Cur.Count=0;
            Cur.Bar=i;
         }
         else if(close[i]<open[i]){
            Cur.Whait=-1;   
            Cur.Count=0;
            Cur.Bar=i;
         }
      }

      if(Cur.Whait!=0){
         Cur.Count++;
         if(Cur.Count>=3){
            double Overlapping=MathMin(high[i],high[i-1])-MathMax(low[i],low[i-1]);
            double PreSize=MathMax(high[i-1]-low[i-1],high[i]-low[i]);
            if(!(Overlapping>=PreSize*MinOverlapping)){
               if(Cur.Count-2>=MinCount){
                  double AverSize,AverBias,AverSizeDif;
                  PatternParameters(high,low,i-1,Cur.Count-2,AverSize,AverBias,AverSizeDif);
                  if(   FormTapered(AverSizeDif,AverSize) ||
                        FormHorizontal(AverSizeDif,AverSize) ||
                        FormExpanding(AverSizeDif,AverSize)
                  ){ 
                     if(Cur.Whait==1){
                        if(CheckInclineForBuy(AverBias/AverSize)){
                           if((EnterAlong && close[i]>open[i]) || (EnterAgainst && close[i]<open[i])){
                              Label1Buffer[i]=low[i];
                              Label3Buffer[i]=close[i]+(high[Cur.Bar]-low[Cur.Bar]);
                           }
                        }
                     }
                     else if(Cur.Whait==-1){
                        if(CheckInclineForSell(AverBias/AverSize)){   
                           if((EnterAlong && close[i]<open[i]) || (EnterAgainst && close[i]>open[i])){
                              Label2Buffer[i]=high[i];                  
                              Label4Buffer[i]=close[i]-(high[Cur.Bar]-low[Cur.Bar]);
                           }
                        }
                     }
                  }
               }
               Cur.Whait=0;
            } 
         }
      }
   }
   
   CheckAlerts(Label1Buffer,Label2Buffer,rates_total,time);
   
   return(rates_total);
}

void PatternParameters( const double & high[],
                        const double & low[],
                        int i,
                        int CurCnt,
                        double & AverSize,
                        double & AverBias,
                        double & AverSizeDif
){
            
   AverSize=high[i-CurCnt]-low[i-CurCnt];
   AverBias=0;
   AverSizeDif=0;
   
   for(int k=i-CurCnt+1;k<i;k++){
      AverSize+=high[k]-low[k];
      double mc=(high[k]+low[k])/2;
      double mp=(high[k-1]+low[k-1])/2;
      AverBias+=(mc-mp);
      double sc=(high[k]-low[k]);
      double sp=(high[k-1]-low[k-1]);
      AverSizeDif+=(sc-sp);               
      
   }
   
   AverSize/=CurCnt;
   AverBias/=(CurCnt-1);
   AverSizeDif/=(CurCnt-1); 
}  

bool FormTapered(double AverDif, double AverSize){
   return(FormTapered && AverDif<-FormTaperedK*AverSize);
}

bool FormHorizontal(double AverDif, double AverSize){
   return(FormRectangular && MathAbs(AverDif)<FormRectangularK*AverSize);
}

bool FormExpanding(double AverDif, double AverSize){
   return(FormExpanding && AverDif>FormExpandingK*AverSize);
}

bool CheckInclineForBuy(double Val){
   return(  (InclineAlong && Val>InclineAlongK) || 
            (InclineHorizontal && MathAbs(Val)<InclineHorizontalK) || 
            (InclineAgainst && Val<-InclineAgainstK)
   );
}   

bool CheckInclineForSell(double Val){
   return(  (InclineAlong && Val<-InclineAlongK) || 
            (InclineHorizontal && MathAbs(Val)<InclineHorizontalK) || 
            (InclineAgainst && Val>InclineAgainstK)
   );
}   

void CheckAlerts( double & UpArrowBuffer[],
                  double & DnArrowBuffer[],
                  int rates_total,
                  const datetime & time[]
){
   if(Alerts!=Alerts_off){
      static datetime tm0=0;
      static datetime tm1=0;
      if(tm0==0){
         tm0=time[rates_total-1];
         tm1=time[rates_total-1];
      }
      string mes="";
      if(UpArrowBuffer[rates_total-Alerts]!=EMPTY_VALUE && 
         tm0!=time[rates_total-1]
      ){
         tm0=time[rates_total-1];
         mes=mes+" buy";
      }
      if(DnArrowBuffer[rates_total-Alerts]!=EMPTY_VALUE && 
         tm1!=time[rates_total-1]
      ){
         tm1=time[rates_total-1];
         mes=mes+" sell";
      } 
      if(mes!=""){
         Alert(MQLInfoString(MQL_PROGRAM_NAME)+"("+Symbol()+","+IntegerToString(PeriodSeconds()/60)+"):"+mes);
      }        
   }   
}
