//---------------------------------------------------------------------
#property copyright 	"Dima S., 2010 г."
#property link      	"dimascub@mail.com"
#property version   	"1.01"
#property description "Индикатор для проверки работы библиотеки TextDisplay."
//---------------------------------------------------------------------
#property indicator_chart_window
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Version history
//---------------------------------------------------------------------
//	07.10.2010г. - V1.00
//	 - FIRST RELEASE
//
//	20.10.2010г. - V1.01
//	 - ADDED - setting angle for table output;
//	 - ADDED - horizontal and vertical shift of the table;
//
//---------------------------------------------------------------------


//---------------------------------------------------------------------
//	Include libraries
//---------------------------------------------------------------------
#include	<TextDisplay.mqh>
//---------------------------------------------------------------------

//=====================================================================
//	External input parameters
//=====================================================================
input ENUM_BASE_CORNER   Corner=CORNER_LEFT_UPPER;
input int               UpDownBorderShift=2;
input int               LeftRightBorderShift=1;
input color               TitlesColor=White;

//---------------------------------------------------------------------

//---------------------------------------------------------------------
TableDisplay      Table1;
//---------------------------------------------------------------------

#define	NUMBER	8
//---------------------------------------------------------------------
string   names[NUMBER]={ "EURUSD","GBPUSD","AUDUSD","NZDUSD","USDCHF","USDCAD","USDJPY","EURJPY" };
int      c1_coord_y[ NUMBER ] = { 0,        1,        2,        3,        4,        5,        6,        7 };
int      c2_coord_y[ NUMBER ] = { 7,        6,        5,        4,        3,        2,        1,        0 };
//---------------------------------------------------------------------
double     rates[NUMBER];
datetime   times[NUMBER];
MqlTick    tick;
//---------------------------------------------------------------------
//	Обработчик события инициализации:
//---------------------------------------------------------------------
int OnInit()
  {
   ArrayInitialize(times,0);
   ArrayInitialize(rates,0);

//	Create table
   Table1.SetParams(0,0,Corner);

//	Show prices
   for(int i=0; i<NUMBER; i++)
     {
      if(Corner==CORNER_LEFT_UPPER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+2,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_LEFT_LOWER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+2,UpDownBorderShift+c2_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_RIGHT_UPPER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+2,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_RIGHT_LOWER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+2,UpDownBorderShift+c2_coord_y[i],Yellow);
        }
      else
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+2,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
     }

//	Show spreads
   for(int i=0; i<NUMBER; i++)
     {
      if(Corner==CORNER_LEFT_UPPER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+4,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_LEFT_LOWER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+4,UpDownBorderShift+c2_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_RIGHT_UPPER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
      else if(Corner==CORNER_RIGHT_LOWER)
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift,UpDownBorderShift+c2_coord_y[i],Yellow);
        }
      else
        {
         Table1.AddFieldObject(40,40,LeftRightBorderShift+4,UpDownBorderShift+c1_coord_y[i],Yellow);
        }
     }

//	Show headers
   for(int i=0; i<NUMBER; i++)
     {
      if(Corner==CORNER_LEFT_UPPER)
        {
         Table1.AddTitleObject(40,40,LeftRightBorderShift,UpDownBorderShift+c1_coord_y[i],names[i]+":",TitlesColor);
        }
      else if(Corner==CORNER_LEFT_LOWER)
        {
         Table1.AddTitleObject(40,40,LeftRightBorderShift,UpDownBorderShift+c2_coord_y[i],names[i]+":",TitlesColor);
        }
      else if(Corner==CORNER_RIGHT_UPPER)
        {
         Table1.AddTitleObject(40,40,LeftRightBorderShift+4,UpDownBorderShift+c1_coord_y[i],names[i]+":",TitlesColor);
        }
      else if(Corner==CORNER_RIGHT_LOWER)
        {
         Table1.AddTitleObject(40,40,LeftRightBorderShift+4,UpDownBorderShift+c2_coord_y[i],names[i]+":",TitlesColor);
        }
      else
        {
         Table1.AddTitleObject(40,40,LeftRightBorderShift,UpDownBorderShift+c2_coord_y[i],names[i]+":",TitlesColor);
        }
     }

   RefreshInfo();
   ChartRedraw(0);

   EventSetTimer(1);

   return(0);
  }
//---------------------------------------------------------------------
//	OnCalculate event handler
//---------------------------------------------------------------------
int OnCalculate(const int rates_total,const int prev_calculated,const int begin,const double &price[])
  {
   return(rates_total);
  }
//---------------------------------------------------------------------
//	OnTimer event handler
//---------------------------------------------------------------------
void OnTimer()
  {
   RefreshInfo();
   ChartRedraw(0);
  }
//---------------------------------------------------------------------
//	OnDeinit event handler
//---------------------------------------------------------------------
void OnDeinit(const int _reason)
  {
   EventKillTimer();

//	Delete table
   Table1.Clear();
  }
//---------------------------------------------------------------------
//	Refresh info
//---------------------------------------------------------------------
void RefreshInfo()
  {
   for(int i=0; i<NUMBER; i++)
     {
      //	Get price data
      ResetLastError();
      if(SymbolInfoTick(names[i],tick)!=true)
        {
         Table1.SetText( i, "Err " + DoubleToString( GetLastError( ), 0 ));
         Table1.SetColor( i, Yellow );
         continue;
        }

      if(tick.time>times[i] || times[i]==0)
        {
         Table1.SetText(i,DoubleToString(tick.bid,(int)(SymbolInfoInteger(names[i],SYMBOL_DIGITS))));
         if(tick.bid>rates[i] && rates[i]>0.1)
           {
            Table1.SetColor(i,Lime);
           }
         else if(tick.bid<rates[i] && rates[i]>0.1)
           {
            Table1.SetColor(i,Red);
           }
         else
           {
            Table1.SetColor(i,Yellow);
           }

         rates[ i ] = tick.bid;
         times[ i ] = tick.time;
        }
      Table1.SetText(i+NUMBER,DoubleToString(( tick.ask-tick.bid)/SymbolInfoDouble(names[i],SYMBOL_POINT),0));
     }
  }
//+------------------------------------------------------------------+
