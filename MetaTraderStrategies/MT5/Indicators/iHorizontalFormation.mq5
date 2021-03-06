#property copyright "Copyright 2016, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 5
#property indicator_plots   5
//--- plot UpArrow
#property indicator_label1  "UpArrow"
#property indicator_type1   DRAW_ARROW
#property indicator_color1  clrAqua
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot DnArrow
#property indicator_label2  "DnArrow"
#property indicator_type2   DRAW_ARROW
#property indicator_color2  clrDeepPink
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//--- plot UpDot
#property indicator_label3  "UpDot"
#property indicator_type3   DRAW_ARROW
#property indicator_color3  clrAqua
#property indicator_style3  STYLE_SOLID
#property indicator_width3  1
//--- plot DnDot
#property indicator_label4  "DnDot"
#property indicator_type4   DRAW_ARROW
#property indicator_color4  clrDeepPink
#property indicator_style4  STYLE_SOLID
#property indicator_width4  1
//--- plot EnterLevel
#property indicator_label5  "EnterLevel"
#property indicator_type5   DRAW_ARROW
#property indicator_color5  clrGray
#property indicator_style5  STYLE_SOLID
#property indicator_width5  1


//--- input parameters

enum ESorce{
   Src_HighLow=0,
   Src_Close=1,
   Src_RSI=2,
   Src_MA=3
};

enum EDirection{
   Dir_NBars=0,
   Dir_CCI=1
};

struct SPeackTrough{
   double   Val;
   int      Dir;
   int      Bar;
};

enum EAlerts{
   Alerts_off=0,
   Alerts_Bar0=1,
   Alerts_Bar1=2
};

enum EPatternType{
   PatternTapered,
   PatternRectangular,
   PatternExpanding
};

enum EInclineType{
   InclineAlong,
   InclineHorizontally,
   InclineAgainst
};

enum EEndType{
   Immediately,
   OneLastVertex,
   TwoLastVertices
};

enum ETargetType{
   FromVertexToVertex,
   OneVertex,
   TwoVertices
};

input EAlerts              Alerts         =  Alerts_off;
input ESorce               SrcSelect      =  Src_HighLow;
input EDirection           DirSelect      =  Dir_NBars;
input int                  RSIPeriod      =  14;
input ENUM_APPLIED_PRICE   RSIPrice       =  PRICE_CLOSE;
input int                  MAPeriod       =  14;
input int                  MAShift        =  0;
input ENUM_MA_METHOD       MAMethod       =  MODE_SMA;
input ENUM_APPLIED_PRICE   MAPrice        =  PRICE_CLOSE;
input int                  CCIPeriod      =  14;
input ENUM_APPLIED_PRICE   CCIPrice       =  PRICE_TYPICAL;
input int                  ZZPeriod       =  14;
input EPatternType         Pattern        =  PatternRectangular;
input EInclineType         Incline        =  InclineHorizontally;     
input double               K1             =  1.5;
input double               K2             =  0.25;
input double               K3             =  0.25;
input int                  N              =  2;
input EEndType             CompletionType =  Immediately;
input ETargetType          Target         =  OneVertex;
input int                  ID             =  1;


int handle=INVALID_HANDLE;

//--- indicator buffers
double         UpArrowBuffer[];
double         DnArrowBuffer[];
double         UpDotBuffer[];
double         DnDotBuffer[];
double         EnterBuffer[];

SPeackTrough PeackTrough[];
int PreCount;
int CurCount;
int PreDir;
int CurDir;

struct SLevelParameters{
   int x1;
   double y1;
   int x2;
   double y2;
   double v;
   int dir;
   double target;
   double y3(int x3){
      if(CompletionType==TwoLastVertices){
            return(y1+(x3-x1)*(y2-y1)/(x2-x1));
      }
      else{
         return(v);
      }
   }
   void Init(){
      x1=0;
      y1=0;
      x2=0;
      y2=0;
      v=0;
      dir=0;   
   }
};


SLevelParameters CurLevel;
SLevelParameters PreLevel;

datetime LastTime;

bool _DrawWaves;

int RequiredCount;

string ShortName;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit(){

   handle=iCustom(Symbol(),Period(),"ZigZags\\iUniZigZagSW",SrcSelect,
                                             DirSelect,
                                             RSIPeriod,
                                             RSIPrice,
                                             MAPeriod,
                                             MAShift,
                                             MAMethod,
                                             MAPrice,
                                             CCIPeriod,
                                             CCIPrice,
                                             ZZPeriod);
                                             
   if(handle==INVALID_HANDLE){
      Alert("Error load indicator");
      return(INIT_FAILED);
   }  
  
   RequiredCount=N*2+2;
  
   SetIndexBuffer(0,UpArrowBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,DnArrowBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,UpDotBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,DnDotBuffer,INDICATOR_DATA);
   SetIndexBuffer(4,EnterBuffer,INDICATOR_DATA);   
   

   PlotIndexSetInteger(0,PLOT_ARROW,233);
   PlotIndexSetInteger(1,PLOT_ARROW,234);
   
   PlotIndexSetInteger(2,PLOT_ARROW,159);
   PlotIndexSetInteger(3,PLOT_ARROW,159);   
   
   PlotIndexSetInteger(4,PLOT_ARROW,159);     
   
   PlotIndexSetInteger(0,PLOT_ARROW_SHIFT,10);
   PlotIndexSetInteger(1,PLOT_ARROW_SHIFT,-10);  
   
   ShortName=MQLInfoString(MQL_PROGRAM_NAME)+"-"+IntegerToString(ID);
   IndicatorSetString(INDICATOR_SHORTNAME,ShortName);

   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason){
   ObjectsDeleteAll(0,MQLInfoString(MQL_PROGRAM_NAME));
   ChartRedraw(0);
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

   int start;
   
   if(prev_calculated==0){
      start=1;      
      CurCount=0;
      PreCount=0;
      CurDir=0;
      PreDir=0;  
      CurLevel.Init();    
      CurLevel.Init();
      LastTime=0;
      
      int ForTester=0;
      if(!(SrcSelect==Src_HighLow)){
         ForTester+=10;
      }   
      if(SrcSelect==Src_HighLow || SrcSelect==Src_Close){
         ForTester+=1;
      }     
      UpArrowBuffer[0]=ForTester;  
      
   }
   else{
      start=prev_calculated-1;
   }

   for(int i=start;i<rates_total;i++){
   
      if(time[i]>LastTime){
         LastTime=time[i];
         PreCount=CurCount;
         PreDir=CurDir;
         PreLevel=CurLevel;
      }
      else{
         CurCount=PreCount;
         CurDir=PreDir;
         CurLevel=PreLevel;
      }
   
      UpArrowBuffer[i]=EMPTY_VALUE;
      DnArrowBuffer[i]=EMPTY_VALUE;
      
      UpDotBuffer[i]=EMPTY_VALUE;
      DnDotBuffer[i]=EMPTY_VALUE;   
      
      EnterBuffer[i]=EMPTY_VALUE; 
      
      double hval[1];
      double lval[1];
      
      double zz[1];
      
      // new max      
      
      double lhb[2];
      if(CopyBuffer(handle,4,rates_total-i-1,2,lhb)<=0){
         return(0);
      }
      if(lhb[0]!=lhb[1]){
         if(CopyBuffer(handle,0,rates_total-i-1,1,hval)<=0){
            return(0);
         }      
         if(CurDir==1){
            RefreshLast(i,hval[0]);
         }
         else{
            AddNew(i,hval[0],1);
         }
      }

      // new min
      
      double llb[2];
      if(CopyBuffer(handle,5,rates_total-i-1,2,llb)<=0){
         return(0);
      }
      if(llb[0]!=llb[1]){
         if(CopyBuffer(handle,1,rates_total-i-1,1,lval)<=0){
            return(0);
         }         
         if(CurDir==-1){
            RefreshLast(i,lval[0]);
         }
         else{
            AddNew(i,lval[0],-1);
         }
      } 
      
      //===
  
      if(CurCount>=RequiredCount){
         if(CurDir!=PreDir){
            int li=CurCount-RequiredCount;
            double base=MathAbs(PeackTrough[li+1].Val-PeackTrough[li+2].Val);
            double l1=MathAbs(PeackTrough[li+1].Val-PeackTrough[li].Val);
            if(l1>=base*K1){
               if(CurDir==1){
                  if(CheckForm(li,base) && CheckInclineForBuy(li,base)){
                     if(CompletionType==Immediately){
                        UpArrowBuffer[i]=low[i];
                        EnterBuffer[i]=close[i];
                        UpDotBuffer[i]=PeackTrough[CurCount-1].Val+l1;
                     }
                     else{
                        SetLevelParameters(1);
                        SetTarget(1,li);
                     }
                  }                
               }
               else if(CurDir==-1){
                  if(CheckForm(li,base) && CheckInclineForSell(li,base)){
                     if(CompletionType==Immediately){
                        DnArrowBuffer[i]=high[i];
                        EnterBuffer[i]=close[i];
                        DnDotBuffer[i]=PeackTrough[CurCount-1].Val-l1;
                     }
                     else{
                        SetLevelParameters(-1);
                        SetTarget(-1,li);
                     }                  
                  }
               }
            }
         }

         if(CompletionType!=Immediately){
            if(PeackTrough[CurCount-1].Bar==i){
               if(CurLevel.dir==1){ // ждем пробоя вверх
                  double cl=CurLevel.y3(i); 
                  if(PeackTrough[CurCount-1].Val>=cl){
                     UpArrowBuffer[i]=low[i];
                     EnterBuffer[i]=cl;
                     if(Target==FromVertexToVertex){
                        UpDotBuffer[i]=CurLevel.target;                        
                     }
                     else{
                        UpDotBuffer[i]=cl+CurLevel.target;
                     }
                     CurLevel.dir=0;
                  }
               }
               else if(CurLevel.dir==-1){
                  double cl=CurLevel.y3(i);
                  if(PeackTrough[CurCount-1].Val<=cl){
                     DnArrowBuffer[i]=low[i];
                     EnterBuffer[i]=cl;
                     if(Target==FromVertexToVertex){
                        DnDotBuffer[i]=CurLevel.target;
                     }
                     else{                     
                        DnDotBuffer[i]=cl-CurLevel.target;
                     }
                     CurLevel.dir=0;
                  }         
               }         
            }
         }         
      }      
   }
   
   CheckAlerts(rates_total,time);
   
   return(rates_total);
}
//+------------------------------------------------------------------+

void RefreshLast(int i,double v){
   PeackTrough[CurCount-1].Bar=i;
   PeackTrough[CurCount-1].Val=v;
} 

void AddNew(int i,double v,int d){
   if(CurCount>=ArraySize(PeackTrough)){
      ArrayResize(PeackTrough,ArraySize(PeackTrough)+1024);
   }
   PeackTrough[CurCount].Dir=d;
   PeackTrough[CurCount].Val=v;
   PeackTrough[CurCount].Bar=i;
   CurCount++;   
   CurDir=d;
} 

// form

bool CheckForm(int li,double base){               
   switch(Pattern){
      case PatternTapered:
         return(CheckFormTapered(li,base));
      break;               
      case PatternRectangular:
         return(CheckFormRectangular(li,base));
      break;
      case PatternExpanding:
         return(CheckFormExpanding(li,base));
      break;
   }
   return(true);
}

bool CheckFormTapered(int li,double base){
   for(int i=1;i<N;i++){
      int j=li+1+i*2;
      double lv=MathAbs(PeackTrough[j].Val-PeackTrough[j+1].Val);
      double lp=MathAbs(PeackTrough[j-2].Val-PeackTrough[j-1].Val);
      if(!(lp-lv>K2*base)){
         return(false);
      }
   } 
   return(true);
}

bool CheckFormRectangular(int li,double base){         
   for(int i=1;i<N;i++){
      int j=li+1+i*2;
      double lv=MathAbs(PeackTrough[j].Val-PeackTrough[j+1].Val);
      if(MathAbs(lv-base)>K2*base){
         return(false);
      }
   }
   return(true);
}

bool CheckFormExpanding(int li,double base){         
   for(int i=1;i<N;i++){
      int j=li+1+i*2;
      double lv=MathAbs(PeackTrough[j].Val-PeackTrough[j+1].Val);
      double lp=MathAbs(PeackTrough[j-2].Val-PeackTrough[j-1].Val);
      if(!(lv-lp>K2*base)){
         return(false);
      }
   }
   return(true);                   
}

// incline

bool CheckInclineForBuy(int li,double base){                 
   switch(Incline){
      case InclineAlong:
         return(CheckInclineUp(li,base));
      break;
      case InclineHorizontally:
         return(CheckInclineHorizontally(li,base));
      break;                     
      case InclineAgainst:
         return(CheckInclineDn(li,base));
      break;
   } 
   return(true);
}  

bool CheckInclineForSell(int li,double base){                 
   switch(Incline){
      case InclineAlong:
         return(CheckInclineDn(li,base));
      break;
      case InclineHorizontally:
         return(CheckInclineHorizontally(li,base));
      break;                     
      case InclineAgainst:
         return(CheckInclineUp(li,base));
      break;
   } 
   return(true);
} 

bool CheckInclineUp(int li,double base){         
   for(int v=1;v<N;v++){
      int vi=li+1+v*2;
      double mc=(PeackTrough[vi].Val+PeackTrough[vi+1].Val)/2;
      double mp=(PeackTrough[vi-2].Val+PeackTrough[vi-1].Val)/2;
      if(!(mc>mp+base*K3)){
         return(false);
      }
   }
   return(true);
} 

bool CheckInclineHorizontally(int li,double base){ 
   double mb=(PeackTrough[li+1].Val+PeackTrough[li+2].Val)/2;        
   for(int v=1;v<N;v++){
      int vi=li+1+v*2;
      double mc=(PeackTrough[vi].Val+PeackTrough[vi+1].Val)/2;
      if(MathAbs(mc-mb)>base*K3){
         return(false);
      }
   }                  
   return(true);
}

bool CheckInclineDn(int li,double base){      
   for(int v=1;v<N;v++){
      int vi=li+1+v*2;
      double mc=(PeackTrough[vi].Val+PeackTrough[vi+1].Val)/2;
      double mp=(PeackTrough[vi-2].Val+PeackTrough[vi-1].Val)/2;
      if(!(mc<mp-base*K3)){
         return(false);
      }
   }
   return(true);
} 

// level

void SetLevelParameters(int dir){
   CurLevel.dir=dir;   
   switch(CompletionType){
      case OneLastVertex:
          CurLevel.v=PeackTrough[CurCount-3].Val;
      break;
      case TwoLastVertices:
         CurLevel.x1=PeackTrough[CurCount-5].Bar;
         CurLevel.y1=PeackTrough[CurCount-5].Val;
         CurLevel.x2=PeackTrough[CurCount-3].Bar;
         CurLevel.y2=PeackTrough[CurCount-3].Val;
      break;
   }
} 

void SetTarget(int dir,int li){
   switch(Target){
      case FromVertexToVertex:
         if(dir==1){
            CurLevel.target=PeackTrough[CurCount-1].Val+(PeackTrough[li+1].Val-PeackTrough[li].Val);
         }
         else if(dir==-1){
            CurLevel.target=PeackTrough[CurCount-1].Val-(PeackTrough[li].Val-PeackTrough[li+1].Val);
         }
      break;
      case OneVertex:
         CurLevel.target=MathAbs(PeackTrough[li].Val-PeackTrough[li+2].Val);
      break;
      case TwoVertices:
         SetTwoVerticesTarget(dir,li);
      break;
   }
}

void SetTwoVerticesTarget(int dir,int li){
   double x11=PeackTrough[li].Bar;
   double y11=PeackTrough[li].Val;
   double x12=PeackTrough[li+1].Bar;
   double y12=PeackTrough[li+1].Val;
   double x21=PeackTrough[li+2].Bar;
   double y21=PeackTrough[li+2].Val;
   double x22=PeackTrough[li+4].Bar;
   double y22=PeackTrough[li+4].Val;
   double t=TwoLinesCrossY(x11,y11,x12,y12,x21,y21,x22,y22);
   if(dir==1){
      CurLevel.target=t-PeackTrough[li].Val;
   }
   else if(dir==-1){
      CurLevel.target=PeackTrough[li].Val-t;         
   }
}

double TwoLinesCrossY(double x11,double y11,double x12,double y12,double x21,double y21,double x22,double y22){
   double k2=(x22-x21)/(y22-y21);
   double k1=(x12-x11)/(y12-y11);
   return((x11-x21-k1*y11+k2*y21)/(k2-k1));
}

// alerts

void CheckAlerts(int rates_total,const datetime & time[]){
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