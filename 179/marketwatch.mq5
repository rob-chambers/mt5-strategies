//---------------------------------------------------------------------
#property copyright 	"Dima S., 2010 ã."
#property link      	"dimascub@mail.com"
#property version   	"1.01"
#property description "Market Watch"
//---------------------------------------------------------------------
#property indicator_chart_window
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Version history:
//---------------------------------------------------------------------
//	11.10.2010. - V1.00
//	 - FIRST RELEASE;
//
//	20.10.2010. - V1.01
//	 - ADDED - parameters for vertical and horizontal shift of the table;
//
//---------------------------------------------------------------------


//---------------------------------------------------------------------
//	Include libraries:
//---------------------------------------------------------------------
#include	<MarketWatch.mqh>
//---------------------------------------------------------------------

//=====================================================================
//	External input parameters:
//=====================================================================
input string   CurrencylList = "EURUSD; GBPUSD; EURGBP; AUDUSD; NZDUSD; AUDNZD; USDJPY; USDCHF; USDCAD; EURJPY; GBPJPY; AUDJPY; NZDJPY; CHFJPY; CADJPY; XAUUSD;";
input string   TimeFrameList = "H1; H4; D1; MN";
input int      UpDownBorderShift=1;
input int      LeftRightBorderShift=1;
input color    TitlesColor=LightCyan;
//---------------------------------------------------------------------

//---------------------------------------------------------------------
string   Symbol_Array[ ];                 // list of symbols
string   TimeFrame_Array[ ];              // list of timeframes
ENUM_TIMEFRAMES   TFs[];
string   Titles_Array[]={ "Bid:","Spread:","StopLev:","ToOpen:","Hi-Lo:","DailyAv:" };
//---------------------------------------------------------------------

//---------------------------------------------------------------------
SymbolWatchDisplay   *Watches[];
TableDisplay         TitlesDisplay;
//---------------------------------------------------------------------

//---------------------------------------------------------------------
int            currencies_count;
int            timeframes_count;
bool         is_first_init=true;
//---------------------------------------------------------------------
#define		WIDTH			128
#define		HEIGHT		128
#define		FONTSIZE	10
//---------------------------------------------------------------------
//	OnInit event handler
//---------------------------------------------------------------------
int OnInit()
  {
//	List of symbols:
   currencies_count=StringToArrayString(CurrencylList,Symbol_Array);
   if(currencies_count>16)
     {
      currencies_count=16;
     }
//	List of timeframes:
   timeframes_count=StringToArrayString(TimeFrameList,TimeFrame_Array);
   ArrayResize(TFs,timeframes_count);
   for(int k=0; k<timeframes_count; k++)
     {
      TFs[k]=get_timeframe_from_string(TimeFrame_Array[k]);
     }

   if(is_first_init!=true)
     {
      DeleteGraphObjects();
     }
   InitGraphObjects();
   is_first_init=false;

   RefreshInfo();
   EventSetTimer(1);

   ChartRedraw(0);

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
   DeleteGraphObjects();
  }
//---------------------------------------------------------------------
//	Initialization of graphic objects
//---------------------------------------------------------------------
void InitGraphObjects()
  {
//Print( "Creating..." );

//	Titles
   TitlesDisplay.SetParams(0,0,CORNER_LEFT_UPPER);
   for(int k=0; k<3; k++)
     {
      TitlesDisplay.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + 5, UpDownBorderShift + k * 2 + 5, Titles_Array[ k ], TitlesColor, "Arial", FONTSIZE );
      TitlesDisplay.SetAnchor( k, ANCHOR_RIGHT );
     }
   TitlesDisplay.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + 5, UpDownBorderShift + 12, Titles_Array[ 3 ], TitlesColor, "Arial", 10 );
   TitlesDisplay.SetAnchor( 3, ANCHOR_RIGHT );
   for(int k=4; k<6; k++)
     {
      TitlesDisplay.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + 5, UpDownBorderShift + 2 * k + 7, Titles_Array[ k ], TitlesColor, "Arial", FONTSIZE );
      TitlesDisplay.SetAnchor( k, ANCHOR_RIGHT );
     }

//	Timeframes
   for(int k=0; k<timeframes_count; k++)
     {
      TitlesDisplay.AddTitleObject( WIDTH, HEIGHT, LeftRightBorderShift + 5, UpDownBorderShift + k * 2 + 20, TimeFrame_Array[ k ] + "%:", TitlesColor, "Arial", FONTSIZE );
      TitlesDisplay.SetAnchor( 6 + k, ANCHOR_RIGHT );
     }

   ArrayResize(Watches,currencies_count);
   for(int i=0; i<currencies_count; i++)
     {
      //	Creating Symbol Watch for every symbol:
      Watches[i]=new SymbolWatchDisplay();
      Watches[i].Create(Symbol_Array[i],0,0,WIDTH,HEIGHT,UpDownBorderShift,LeftRightBorderShift+6+i*7,get_currency_color(Symbol_Array[i]),TFs);
     }
  }
//---------------------------------------------------------------------
//	Delete graphic objects
//---------------------------------------------------------------------
void DeleteGraphObjects()
  {
//Print( "Delete..." );

   TitlesDisplay.Clear();
   for(int i=0; i<currencies_count; i++)
     {
      if(CheckPointer(Watches[i])!=POINTER_INVALID)
        {
         //	Delete element for one symbol:
         delete(Watches[i]);
        }
     }
  }
//---------------------------------------------------------------------
//	Refresh symbol information:
//---------------------------------------------------------------------
void RefreshInfo()
  {
   for(int i=0; i<currencies_count; i++)
     {
      Watches[ i ].RefreshSymbolInfo( );
      Watches[ i ].RedrawSymbolInfo( );
     }
  }
//+----------------------------------------------------------------------------+
//|  Author   : Kim Igor aka KimIV,  http://www.kimiv.ru                       |
//!  Modified : Dima S., 2010 ã.                                               !
//+----------------------------------------------------------------------------+
//|  Version  : 10.10.2008                                                     |
//|  Descr.   : It parses the string with words into the array                 |
//+----------------------------------------------------------------------------+
//|  Parameters:                                                               |
//|    st - string with words, separated with specified delimiter              |
//|    ad - array of words                                                     |
//+----------------------------------------------------------------------------+
//|  Returned value:                                                           |
//|    Number of elements in array                                             |
//+----------------------------------------------------------------------------+
int StringToArrayString(string st,string &ad[],string _delimiter=";")
  {
   int      i=0,np;
   string   stp;

   ArrayResize(ad,0);
   while(StringLen(st)>0)
     {
      np=StringFind(st,_delimiter);
      if(np<0)
        {
         stp= st;
         st = "";
        }
      else
        {
         stp= StringSubstr(st,0,np);
         st = StringSubstr(st,np+1);
        }
      i++;
      ArrayResize(ad,i);
      StringTrimLeft(stp);
      ad[i-1]=stp;
     }

   return(ArraySize(ad));
  }
//---------------------------------------------------------------------
//	Get color depending on symbol
//---------------------------------------------------------------------
color get_currency_color(string _currency)
  {
   int      i;
   for(i=0; i<currencies_count; i++)
     {
      if(StringFind(Symbol_Array[i],_currency)!=-1)
        {
         if(StringFind(_currency,"GOLD")!=-1)
           {
            return(Gold);
           }
         else if(StringFind(_currency,"XAU")!=-1)
           {
            return(Gold);
           }
         else if(StringFind(_currency,"JPY")!=-1)
           {
            return(NavajoWhite);
           }
         else if(StringFind(_currency,"EUR")!=-1)
           {
            return(DeepSkyBlue);
           }
         else if(StringFind(_currency,"GBP")!=-1)
           {
            return(DeepSkyBlue);
           }
         else if(StringFind(_currency,"QM")!=-1)
           {
            return(Brown);
           }
         else if(StringFind(_currency,"ES")!=-1)
           {
            return(LightSalmon);
           }
         else if(StringFind(_currency,"NQ")!=-1)
           {
            return(LightSalmon);
           }
         else if(StringFind(_currency,"CHF")!=-1)
           {
            return(SpringGreen);
           }
         else if(StringFind(_currency,"CAD")!=-1)
           {
            return(SpringGreen);
           }
         else if(StringFind(_currency,"AUD")!=-1)
           {
            return(GreenYellow);
           }
         else if(StringFind(_currency,"NZD")!=-1)
           {
            return(GreenYellow);
           }

         return(Silver);
        }
     }
   return(Silver);
  }
//---------------------------------------------------------------------
//	Converts string with timeframe into integer value
//---------------------------------------------------------------------
ENUM_TIMEFRAMES get_timeframe_from_string(string _str)
  {
   if(_str=="M1")
     {
      return(PERIOD_M1);
     }
   if(_str=="M2")
     {
      return(PERIOD_M2);
     }
   if(_str=="M3")
     {
      return(PERIOD_M3);
     }
   if(_str=="M4")
     {
      return(PERIOD_M4);
     }
   if(_str=="M5")
     {
      return(PERIOD_M5);
     }
   if(_str=="M6")
     {
      return(PERIOD_M6);
     }
   if(_str=="M10")
     {
      return(PERIOD_M10);
     }
   if(_str=="M12")
     {
      return(PERIOD_M12);
     }
   if(_str=="M15")
     {
      return(PERIOD_M15);
     }
   if(_str=="M20")
     {
      return(PERIOD_M20);
     }
   if(_str=="M30")
     {
      return(PERIOD_M30);
     }
   if(_str=="H1")
     {
      return(PERIOD_H1);
     }
   if(_str=="H2")
     {
      return(PERIOD_H2);
     }
   if(_str=="H3")
     {
      return(PERIOD_H3);
     }
   if(_str=="H4")
     {
      return(PERIOD_H4);
     }
   if(_str=="H6")
     {
      return(PERIOD_H6);
     }
   if(_str=="H8")
     {
      return(PERIOD_H8);
     }
   if(_str=="H12")
     {
      return(PERIOD_H12);
     }
   if(_str=="D1")
     {
      return(PERIOD_D1);
     }
   if(_str=="W1")
     {
      return(PERIOD_W1);
     }
   if(_str=="MN1")
     {
      return(PERIOD_MN1);
     }

   return(PERIOD_D1);
  }
//+------------------------------------------------------------------+
