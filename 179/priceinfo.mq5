//---------------------------------------------------------------------
#property copyright 	"Dima S., 2010 г."
#property link      	"dimascub@mail.com"
#property version   	"1.00"
#property description "Отображение параметров символа"
//---------------------------------------------------------------------
#property indicator_chart_window
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Version History
//---------------------------------------------------------------------
//	11.10.2010г. - V1.00
//	 - FIRST RELEASE;
//
//	20.10.2010г. - V1.01
//	 - ADDED - setting angle for table output;
//	 - ADDED - horizontal and vertical shift of the table;
//
//---------------------------------------------------------------------


//---------------------------------------------------------------------
//	Include library
//---------------------------------------------------------------------
#include	<TextDisplay.mqh>
//---------------------------------------------------------------------

//=====================================================================
//	External input parameters
//=====================================================================
input ENUM_BASE_CORNER   Corner=CORNER_RIGHT_UPPER;
input int                UpDownBorderShift=1;
input int                LeftRightBorderShift=1;
input color              TitlesColor=LightCyan;

//---------------------------------------------------------------------

//---------------------------------------------------------------------
TableDisplay      Table1;
//---------------------------------------------------------------------

#define	NUMBER	6
//---------------------------------------------------------------------
string   titles[]={ "Spread:","Stop Level:","Pips to Open:","Hi to Lo:","Daily Av:"  };
int         c1_coord_y[ NUMBER ] = { 1,        3,        4,        6,        7,        8 };
int         c2_coord_y[ NUMBER ] = { 9,        7,        6,        4,        3,        2 };
int         c1_coord_x[ NUMBER ] = { 2,        4,        4,        4,        4,        4 };
int         c2_coord_x[ NUMBER ] = { 4,        2,        2,        2,        2,        2 };
//---------------------------------------------------------------------
double      rates= 0.0;
datetime   times = 0;
MqlTick      tick;
MqlRates   bars[6];
//---------------------------------------------------------------------
#define		WIDTH			50
#define		HEIGHT		60
#define		FONTSIZE	12
//---------------------------------------------------------------------
//	OnInit event handler
//---------------------------------------------------------------------
int OnInit()
  {
//	Create table
   Table1.SetParams(0,0,Corner);

//	Show prices
   for(int i=0; i<NUMBER; i++)
     {
      if(Corner==CORNER_LEFT_UPPER)
        {
         Table1.AddFieldObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ] + 2, UpDownBorderShift + c1_coord_y[ i ], Gold, "Arial", FONTSIZE );
         Table1.SetAnchor( i, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_LEFT_LOWER)
        {
         Table1.AddFieldObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ] + 2, UpDownBorderShift + c2_coord_y[ i ], Gold, "Arial", FONTSIZE );
         Table1.SetAnchor( i, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_RIGHT_UPPER)
        {
         Table1.AddFieldObject( WIDTH, HEIGHT, LeftRightBorderShift + c2_coord_x[ i ] - 2, UpDownBorderShift + c1_coord_y[ i ], Gold, "Arial", FONTSIZE );
         Table1.SetAnchor( i, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_RIGHT_LOWER)
        {
         Table1.AddFieldObject( WIDTH, HEIGHT, LeftRightBorderShift + c2_coord_x[ i ] - 2, UpDownBorderShift + c2_coord_y[ i ], Gold, "Arial", FONTSIZE );
         Table1.SetAnchor( i, ANCHOR_RIGHT );
        }
      else
        {
         Table1.AddFieldObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ] + 2, UpDownBorderShift + c1_coord_y[ i ], Gold, "Arial", FONTSIZE );
         Table1.SetAnchor( i, ANCHOR_RIGHT );
        }
     }
   Table1.SetFont( 0, "Arial", 20 );
   Table1.SetAnchor( 0, ANCHOR_CENTER );

//	Show headers
   for(int i=1; i<NUMBER; i++)
     {
      if(Corner==CORNER_LEFT_UPPER)
        {
         Table1.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ], UpDownBorderShift + c1_coord_y[ i ], titles[ i - 1 ],  TitlesColor, "Arial", FONTSIZE );
         Table1.SetAnchor( NUMBER + i - 1, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_LEFT_LOWER)
        {
         Table1.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ], UpDownBorderShift + c2_coord_y[ i ], titles[ i - 1 ],  TitlesColor, "Arial", FONTSIZE );
         Table1.SetAnchor( NUMBER + i - 1, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_RIGHT_UPPER)
        {
         Table1.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + c2_coord_x[ i ], UpDownBorderShift + c1_coord_y[ i ], titles[ i - 1 ],  TitlesColor, "Arial", FONTSIZE );
         Table1.SetAnchor( NUMBER + i - 1, ANCHOR_RIGHT );
        }
      else if(Corner==CORNER_RIGHT_LOWER)
        {
         Table1.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + c2_coord_x[ i ], UpDownBorderShift + c2_coord_y[ i ], titles[ i - 1 ],  TitlesColor, "Arial", FONTSIZE );
         Table1.SetAnchor( NUMBER + i - 1, ANCHOR_RIGHT );
        }
      else
        {
         Table1.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + c1_coord_x[ i ], UpDownBorderShift + c2_coord_y[ i ], titles[ i - 1 ],  TitlesColor, "Arial", FONTSIZE );
         Table1.SetAnchor( NUMBER + i - 1, ANCHOR_RIGHT );
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
//	OnDeInit event handler
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
//	Get price data
   ResetLastError();
   if(SymbolInfoTick(Symbol(),tick)!=true)
     {
      Table1.SetText( 0, "Err " + DoubleToString( GetLastError( ), 0 ));
      Table1.SetColor( 0, Yellow );
      return;
     }

//	If the data has changed, show the recent values
   if(tick.time>times || times==0)
     {
      Table1.SetText(0,DoubleToString(tick.bid,(int)(SymbolInfoInteger(Symbol(),SYMBOL_DIGITS))));
      if(tick.bid>rates && rates>0.1)
        {
         Table1.SetColor(0,Lime);
        }
      else if(tick.bid<rates && rates>0.1)
        {
         Table1.SetColor(0,Red);
        }
      else
        {
         Table1.SetColor(0,Yellow);
        }

      rates = tick.bid;
      times = tick.time;
     }
   Table1.SetText( 1, DoubleToString(( tick.ask - tick.bid ) / SymbolInfoDouble( Symbol( ), SYMBOL_POINT ), 0 ));
   Table1.SetText( 2, DoubleToString( SymbolInfoInteger( Symbol( ), SYMBOL_TRADE_STOPS_LEVEL ), 0 ));

   CopyRates(Symbol(),PERIOD_D1,0,6,bars);

//	Points from day open price
   if(bars[5].close>bars[5].open)
     {
      Table1.SetColor( 3, Lime );
      Table1.SetText( 3, DoubleToString(( bars[ 5 ].close - bars[ 5 ].open ) / SymbolInfoDouble( Symbol( ), SYMBOL_POINT ), 0 ));
     }
   else if(bars[5].close<bars[5].open)
     {
      Table1.SetColor( 3, Red );
      Table1.SetText( 3, DoubleToString(( bars[ 5 ].open - bars[ 5 ].close ) / SymbolInfoDouble( Symbol( ), SYMBOL_POINT ), 0 ));
     }
   else
     {
      Table1.SetText( 3, "0" );
      Table1.SetColor( 3, Yellow );
     }

//	Current daily range
   Table1.SetText(4,DoubleToString(( bars[5].high-bars[5].low)/SymbolInfoDouble(Symbol(),SYMBOL_POINT),0));

//	Price range averaged (previous 5 days)
   double   av=0.0;
   for(int i=0; i<5; i++)
     {
      av+=bars[i].high-bars[i].low;
     }
   av/=(SymbolInfoDouble(Symbol(),SYMBOL_POINT)*5.0);
   Table1.SetText(5,DoubleToString(av,0));
  }
//+------------------------------------------------------------------+
