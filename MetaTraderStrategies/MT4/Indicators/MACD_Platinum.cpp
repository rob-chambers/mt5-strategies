//+------------------------------------------------------------------+
//|                                            PIP-F_MACD_Platinum.mq4 |
//| Copyright © 2010 pip-factory.com  Coded by TradingCoders.com |
//|                                   http://www.pip-factory.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, pip-factory P/L."
#property link      "http://www.pip-factory.com"
//----
#property indicator_separate_window
#property indicator_buffers 8
#property indicator_color1 Green
#property indicator_color2 LimeGreen
#property indicator_color3 Red
#property indicator_color4 FireBrick
#property indicator_color5 RoyalBlue
#property indicator_color6 IndianRed
#property indicator_color7 Turquoise
#property indicator_color8 OrangeRed

#property indicator_style1 STYLE_SOLID
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
#property indicator_style4 STYLE_SOLID
#property indicator_style5 STYLE_DOT
#property indicator_style6 STYLE_SOLID
#property indicator_style7 STYLE_SOLID
#property indicator_style8 STYLE_SOLID

#property indicator_width1 2
#property indicator_width2 2
#property indicator_width3 2
#property indicator_width4 2
#property indicator_width6 2
#property indicator_width7 1
#property indicator_width8 1

//---- input parameters
extern int  Fast = 12;
extern int  Slow = 26;
extern int  Smooth = 9;
extern bool ZeroLag = true;
extern bool   ShowMarkers = false;
extern bool DiffHistogram = false;
extern string AlertSound = "alert.wav";
extern bool   AlertSoundEnabled = false;
extern bool   AlertEnabled = false;
extern bool   AlertZeroCrossEnabled = false;
extern int   SignalMode = 1;
extern int   MarkerSize = 1;
extern color ZeroLineCrossColor = MediumOrchid;
extern color MacdColor = RoyalBlue;
extern color AvgColor = IndianRed;
extern color UpwardsAboveZeroColor = Green;
extern color UpwardsBelowZeroColor = LimeGreen;
extern color DownwardsAboveZeroColor = Red;
extern color DownwardsBelowZeroColor = FireBrick;
extern color MarkerColorUp = Turquoise;
extern color MarkerColorDown = OrangeRed;

// ADDITIONAL TOOL FOR RENKO, RANGE ETC OFFLINE CHARTS ########################################################
 bool OfflineChart = false;
 
//---- buffers
double HistUpAbove[];      // = 0
double HistDnAbove[];      // = 1
double HistUpBelow[];      // = 2
double HistDnBelow[];      // = 3
double Macd[];             // = 4
double Avg[];              // = 5
double MarkersUp[];        // = 6
double MarkersDown[];      // = 7

// series arrays
double fastEma[1];
double slowEma[1];
double Diff[1];
double avgEma[1];   
double fastEmaEma[1];
double slowEmaEma[1];
double avgEmaEma[1];
   // for adxvma
       double PDI[1];
		 double PDM[1];
		 double MDM[1];
		 double MDI[1];
		 double Out[1];
		 double ADXVMAPlot[1];
		 double dotSeries[1];
		 
		 double HHV = -99999999; //double.MinValue;
		 double LLV = +99999999; //double.MaxValue;
// various globals
int GrID = 0;     // used as graphic ID
string short_name;
int   BarsMem = 0;
int Panel = 1;
bool bInit = false;
bool inittedThisBar = false;
int IndicatorCountedMem = 0;
int   Multiplier = 10;   // forex multiplier from Ninja version
int   LatestBarComplete = 0;  // remembers the latest bar processed.
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   IndicatorBuffers(8);
   
   
//---- indicator lines

   SetIndexStyle(0, DRAW_HISTOGRAM,EMPTY,EMPTY,UpwardsAboveZeroColor);
   SetIndexStyle(1, DRAW_HISTOGRAM,EMPTY,EMPTY,DownwardsAboveZeroColor);
   SetIndexBuffer(0, HistUpAbove);
   SetIndexBuffer(1, HistDnAbove);
   SetIndexStyle(2, DRAW_HISTOGRAM,EMPTY,EMPTY,UpwardsBelowZeroColor);
   SetIndexStyle(3,  DRAW_HISTOGRAM,EMPTY,EMPTY,DownwardsBelowZeroColor);
   SetIndexBuffer(2, HistUpBelow); 
   SetIndexBuffer(3, HistDnBelow); 
   SetIndexBuffer(4, Macd);
   SetIndexBuffer(5, Avg);
   SetIndexStyle(4, DRAW_LINE,STYLE_DOT,EMPTY, MacdColor);
   SetIndexStyle(5, DRAW_LINE,EMPTY,EMPTY, AvgColor);
   SetIndexBuffer(6, MarkersUp);
   SetIndexStyle(6,  DRAW_ARROW,EMPTY,MarkerSize,MarkerColorUp);  
   SetIndexBuffer(7, MarkersDown);
   SetIndexStyle(7, DRAW_ARROW,EMPTY,MarkerSize,MarkerColorDown);
   SetIndexArrow(6,108);
   SetIndexArrow(7,108);
//---- various Init() stuff
   if (Fast<=0)
      Fast = 12;
   if (Slow <=0)
      Slow = 26;
   if (Smooth <=0)
      Smooth = 9;
   if (MarkerSize <= 0)
      MarkerSize = 1;
   if (SignalMode > 2 || SignalMode < 1)
      SignalMode = 1;   
   if (AlertSound == "")
      AlertSound = "alert.wav";

               
     // array series  
   ArraySetAsSeries(fastEma,true);      // set this as a series
   ArraySetAsSeries(slowEma,true);
   ArraySetAsSeries(Diff,true);
   ArraySetAsSeries(avgEma,true);
   ArraySetAsSeries(fastEmaEma,true);
   ArraySetAsSeries(slowEmaEma,true);
   ArraySetAsSeries(avgEmaEma,true);
   ArraySetAsSeries(PDI,true);
   ArraySetAsSeries(PDM,true);
   ArraySetAsSeries(MDM,true);
   ArraySetAsSeries(MDI,true);
   ArraySetAsSeries(Out,true);
   ArraySetAsSeries(ADXVMAPlot,true);
   ArraySetAsSeries(dotSeries,true);
   
//----
   int Length = Slow;
  SetIndexDrawBegin(0, Length);
  SetIndexDrawBegin(1, Length);
  SetIndexDrawBegin(2, Length);
  SetIndexDrawBegin(3, Length);
  SetIndexDrawBegin(4, Length);
  SetIndexDrawBegin(5, Length);
  SetIndexDrawBegin(6, Length);
  SetIndexDrawBegin(7, Length);
  
//---- name for DataWindow and indicator subwindow label
   short_name="PIP-F_MACD_Platinum(" + Fast + ", " + Slow + ", " + Smooth +")";
   IndicatorShortName(short_name);
   // >>>>> ADD WINDOW PANEL TO ID, TO ASSIST MULTIPLE INSTANCES OF SAME INDICATOR >>>
   int w = WindowFind(short_name);
   short_name = short_name + w;
   IndicatorShortName(short_name);
   // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
   Panel = w;  // remember this panel now, because later on it WindowFind fails. (stupidly and buggily)
   
   SetIndexLabel(0,"Diff_UpAbove");
   SetIndexLabel(1,"Diff_DnAbove");
   SetIndexLabel(2,"Diff_UpBelow");
   SetIndexLabel(3,"Diff_DnBelow");
   SetIndexLabel(4,"Macd");
   SetIndexLabel(5,"Avg");
   SetIndexLabel(6,"");
   SetIndexLabel(7,"");
//----
   bInit = false;
   BarsMem = 0;
   IndicatorCountedMem = 0;
   LatestBarComplete = 0;
   return(0);
  }
//+------------------------------------------------------------------+
//| deinit()                                                         |
//+------------------------------------------------------------------+ 
int deinit()
{
   DoObjectsDeleteAll();
   return (0);
}  
//+------------------------------------------------------------------+
//| PIP-F_MACD_Platinum MAIN()                                         |
//+------------------------------------------------------------------+
int start()
  {
   // create and organise our NewBar flag.  
   if (BarsMem == 0)
      BarsMem = Bars;
   bool NewBar = false;
   if (BarsMem != Bars)
   {
      NewBar = true;
      BarsMem = Bars;
      //Print("New Bar at bar ",Bars);
   }
  
   // TEST EACH TICK FOR OFFLINE STATUS ########################################################
   if (IndicatorCounted() == 0)
      OfflineChart = true;
   else
      OfflineChart = false;
   // ##########################################################################################
     

   inittedThisBar = false;
   if (!bInit)
   {
      Print("bInit");
      //---- name for DataWindow and indicator subwindow label
   short_name="PIP-F_MACD_Platinum(" + Fast + ", " + Slow + ", " + Smooth +")";
   IndicatorShortName(short_name);
   // >>>>> ADD WINDOW PANEL TO ID, TO ASSIST MULTIPLE INSTANCES OF SAME INDICATOR >>>
   int w = WindowFind(short_name);
   short_name = short_name + w;
   IndicatorShortName(short_name);
   // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
   Panel = w;  // remember this panel now, because later on it WindowFind fails. (stupidly and buggily)
   
      ArrayResize(fastEma,1);      // reset this 
      ArrayResize(slowEma,1);
      ArrayResize(Diff,1);
      ArrayResize(avgEma,1);
      ArrayResize(fastEmaEma,1);
      ArrayResize(slowEmaEma,1);
      ArrayResize(avgEmaEma,1);
      ArrayResize(PDI,1);
      ArrayResize(PDM,1);
      ArrayResize(MDM,1);
      ArrayResize(MDI,1);
      ArrayResize(Out,1);
      ArrayResize(ADXVMAPlot,1);
      ArrayResize(dotSeries,1);
   
      inittedThisBar = true;
      bInit = true;
   }
   
   int Length = Slow;  
  
   int    i, k, counted_bars = IndicatorCounted();
    // COPE WITH OFFLINE CHARTS, which IndicatorCounted() returns zero always #############################################
   if (OfflineChart)
   {
      i = IndicatorCountedMem;
      k = i;
      counted_bars = i;
   }
   
   
   if (counted_bars < 1 || inittedThisBar)
   {
       for(i = 1; i < Bars; i++) 
       {
            
           HistUpAbove[Bars-i] = 0.0;
           HistDnAbove[Bars-i] = 0.0;
           HistUpBelow[Bars-i] = 0.0;
           HistDnBelow[Bars-i] = 0.0;
           Macd[Bars-i] = 0.0;
           Avg[Bars-i] = 0.0;
           MarkersUp[Bars-i] = 0.0;
           MarkersDown[Bars-i] = 0.0;
       }
       
       LatestBarComplete = 0;
           
       // initialise Series Arrays
       ArrayResize(fastEma,Bars);
       ArrayResize(slowEma,Bars);
       ArrayResize(Diff,Bars);
       ArrayResize(avgEma,Bars);
       ArrayResize(fastEmaEma,Bars);
       ArrayResize(slowEmaEma,Bars);
       ArrayResize(avgEmaEma,Bars);
      ArrayResize(PDI,Bars);
      ArrayResize(PDM,Bars);
      ArrayResize(MDM,Bars);
      ArrayResize(MDI,Bars);
      ArrayResize(Out,Bars);
      ArrayResize(ADXVMAPlot,Bars);
      ArrayResize(dotSeries,1);
       ArrayInitialize(fastEma,0);
       ArrayInitialize(slowEma,0);
       ArrayInitialize(Diff,0);
       ArrayInitialize(avgEma,0);
       ArrayInitialize(slowEmaEma,0);
       ArrayInitialize(fastEmaEma,0);
       ArrayInitialize(avgEmaEma,0);
      ArrayInitialize(PDI,0);
      ArrayInitialize(PDM,0);
      ArrayInitialize(MDM,0);
      ArrayInitialize(MDI,0);
      ArrayInitialize(Out,0);
      ArrayInitialize(ADXVMAPlot,0);
      ArrayInitialize(dotSeries,1);
        
   }
   // ------------ inserting here general reset upon sudden influx of uncounted Bars ----------------
      if ( (IndicatorCountedMem != 0 && counted_bars > IndicatorCountedMem+10)
      || inittedThisBar)
      {
         // it seems we have more than just a simple increment of one new bar. Do a full reset
         DoObjectsDeleteAll();
         Print("Resetting "+short_name);
         i=0;k=0;counted_bars=0;IndicatorCountedMem=0; BarsMem=0;   
         
         // initialise Series Arrays
       //ArrayResize(fastEma,1);
       //ArrayResize(slowEma,1);
       //ArrayResize(Diff,1); 
       //ArrayResize(avgEma,1);
       //ArrayResize(fastEmaEma,1);
       //ArrayResize(slowEmaEma,1);
       //ArrayResize(avgEmaEma,1);
      //ArrayResize(PDI,Bars);
      //ArrayResize(PDM,Bars);
      //ArrayResize(MDM,Bars);
      //ArrayResize(MDI,Bars);
      //ArrayResize(Out,Bars);
      //ArrayResize(ADXVMAPlot,Bars);
      //ArrayResize(dotSeries,Bars);
      }
      // Changed test for OFFLINE CHARTS ##################################################################################
      if (OfflineChart)
         IndicatorCountedMem = MathMax(IndicatorCountedMem,IndicatorCounted());  // MODIFIED TO COPE WITH OFFLINE CHARTS
      else
         IndicatorCountedMem = IndicatorCounted();
      // ###################################################################################################################
      

   //double price, sum, mul; 
   if(Length <= 1)
       return(0);
   if(Bars <= Length) 
       return(0);
       
//---- initial zero
   if(counted_bars < 1)
   {
      
       
       //Print("Range of array index 0 is ",ArrayRange(fastEma,0));   
       inittedThisBar = true; 
       
       SetLevelValue(0,0);    // ZL
       SetLevelStyle(STYLE_SOLID,1,DarkGray);
        
    }
//---- last counted bar will be recounted
   int limit = Bars - counted_bars;
   if(counted_bars > 0) 
       limit++;
       
  // Series Arrays length updates - elegant
       UpsizeSeriesArray(fastEma);
       UpsizeSeriesArray(slowEma);
       UpsizeSeriesArray(Diff);
       UpsizeSeriesArray(avgEma);
       UpsizeSeriesArray(fastEmaEma);
       UpsizeSeriesArray(slowEmaEma);
       UpsizeSeriesArray(avgEmaEma);
         UpsizeSeriesArray(PDI);
         UpsizeSeriesArray(PDM);
         UpsizeSeriesArray(MDM);
         UpsizeSeriesArray(MDI);
         UpsizeSeriesArray(Out);
         UpsizeSeriesArray(ADXVMAPlot);
         UpsizeSeriesArray(dotSeries);
        

//---- 


// MAIN TRADING CODERS LOOP OF AUXILLIARY THINGS HERE
   i = Bars - Length + 1;
   if(counted_bars > Length - 1) 
       i = Bars - counted_bars - 1;
   while(i >= 0)
     {
       LatestBarComplete = MathMax(LatestBarComplete,Bars-i);
       
       //if (Bars-i >= LatestBarComplete)
       {
         double macd = 0;
         double macdAvg = 0;
         
         if (!ZeroLag)
         {
            fastEma[i] = ((2.0 / (1 + Fast)) * (Multiplier*Close[i+0]) + (1 - (2.0 / (1 + Fast))) * fastEma[i+1]);
				slowEma[i] = ((2.0 / (1 + Slow)) * (Multiplier*Close[i+0]) + (1 - (2.0 / (1 + Slow))) * slowEma[i+1]);

				macd		= fastEma[i+0] - slowEma[i+0];
				macdAvg	= (2.0 / (1 + Smooth)) * macd + (1 - (2.0 / (1 + Smooth))) * Avg[i+1];
				
				Macd[i] = (macd);
				Avg[i] = (macdAvg);
				Diff[i] = (macd - macdAvg);
		   }
		   else
		   {	
		   // zerolag. 
		       // do lag calculations first
		        fastEma[i] = ((2.0 / (1 + Fast)) * (Multiplier*Close[i+0]) + (1 - (2.0 / (1 + Fast))) * fastEma[i+1]);
				  slowEma[i] = ((2.0 / (1 + Slow)) * (Multiplier*Close[i+0]) + (1 - (2.0 / (1 + Slow))) * slowEma[i+1]);
             // calculate the EMA of these EMAs
             //double fastEmaEma = iMAOnArray(fastEma,0,Fast,0,MODE_EMA,i);
             //double slowEmaEma = iMAOnArray(slowEma,0,Slow,0,MODE_EMA,i);
             fastEmaEma[i] = (2.0 / (1 + Fast)) * fastEma[i] + (1 - (2.0 / (1 + Fast))) * fastEmaEma[i+1];
             slowEmaEma[i] = (2.0 / (1 + Slow)) * slowEma[i] + (1 - (2.0 / (1 + Slow))) * slowEmaEma[i+1];
             double differenceFast = fastEma[i] - fastEmaEma[i];
             double differenceSlow = slowEma[i] - slowEmaEma[i];
             macd = (  (fastEma[i]+differenceFast) - (slowEma[i]+differenceSlow)   );             
             Macd[i] = (macd);
             //avgEma[i] = iMAOnArray(Macd,0,Smooth,0,MODE_EMA,i);
             //double avgEmaEma = iMAOnArray(avgEma,0,Smooth,0,MODE_EMA,i);
             //double differenceAvg = avgEma[i] - avgEmaEma;
             //macdAvg = avgEma[i] + differenceAvg;
             // temp test
             avgEma[i] =      (2.0 / (1 + Smooth)) * Macd[i]   + (1 - (2.0 / (1 + Smooth))) * avgEma[i+1];
			    avgEmaEma[i] =   (2.0 / (1 + Smooth)) * avgEma[i] + (1 - (2.0 / (1 + Smooth))) * avgEmaEma[i+1];
             double differenceAvg = avgEma[i] - avgEmaEma[i];
             macdAvg = avgEma[i] + differenceAvg;
             Avg[i] = (macdAvg);
             Diff[i] = (macd-macdAvg);
             
           
         
		   }

		 }
		 	
       // plot into the four different Buffers
       if (DiffHistogram)
       {
         HistUpAbove[i] = 0.0;
         HistDnAbove[i] = 0.0;
         HistUpBelow[i] = 0.0;
         HistDnBelow[i] = 0.0;
         if (Diff[i] > Diff[i+1] && Diff[i] > 0.0)
              HistUpAbove[i] = Diff[i];
         else if (Diff[i] > Diff[i+1] && Diff[i] <= 0.0)
              HistUpBelow[i] = Diff[i];
         else if (Diff[i] <= Diff[i+1] && Diff[i] <= 0.0)
              HistDnBelow[i] = Diff[i];
         else if (Diff[i] <= Diff[i+1] && Diff[i] > 0.0)
              HistDnAbove[i] = Diff[i];
        }
        
       //
       
       
       // draw signals
       if (i==1 && AlertEnabled)
         Comment("");   // delete any current comment if we make any
         
       // markers
       if (ShowMarkers && i>= 1 &&
            Macd[i] > Avg[i] && ( (Macd[i+1] < Avg[i+1]) || (Macd[i+1] == Avg[i+1] && Macd[i+2] < Avg[i+2]) )   )
            MarkersUp[i] = Avg[i];
       else
            MarkersUp[i] = EMPTY_VALUE;
       if (ShowMarkers && i>= 1 &&
            Macd[i] < Avg[i] && ( (Macd[i+1] > Avg[i+1]) || (Macd[i+1] == Avg[i+1] && Macd[i+2] > Avg[i+2]) )   )
            MarkersDown[i] = Avg[i];
       else
            MarkersDown[i] = EMPTY_VALUE;
         
               
       // ORIGINAL UNFUNKY MACD/SIGNAL CROSS DOTS.
       if (AlertZeroCrossEnabled && i==1)
       {
            if (MarkersUp[i] == Avg[i])
            {
               Comment("MACD Cross Long Signal");
               Print("MACD Cross Long Signal");
               if (AlertSoundEnabled)
                  PlaySound(AlertSound);
            }
            else if (MarkersDown[i] == Avg[i])
            {
               Comment("MACD Cross Short Signal");
               Print("MACD Cross Short Signal");
               if (AlertSoundEnabled)
                  PlaySound(AlertSound);
            }
       } 
       
        
       // MACD_PLATINUM ADDITIONAL GRAPHICS :
       
       // 1) fill in band between MACD line and AVG line. Limited resources in MT4. Create graphic objects
       DoBandFill(i);
       
       // 2) Marking of dots along zero line.
			double dotSeriesVal = 0;
			if (SignalMode == 1)
			{
				// uses an ADX line
				double adxvma = DoADXVMA(Fast,i);  // keyed permanently to MACD series buffer	
				if (Macd[i] > 0.0 && Macd[i] > adxvma)
					dotSeriesVal = 1;
				else if (Macd[i] < 0.0 && Macd[i] < adxvma)
					dotSeriesVal = -1;
				else dotSeriesVal = 0;		
			}
			else if (SignalMode == 2)
			{
				// tests against zero line	
				if (Macd[i] > Avg[i] && Macd[i] > 0)
					dotSeriesVal = 1;
				else if (Macd[i] < Avg[i] && Macd[i] < 0)
					dotSeriesVal = -1;
				else dotSeriesVal = 0;		
			}
			DoDotSeries(dotSeriesVal,i); // puts a colored dot along the zero line.
			dotSeries[i] = dotSeriesVal;
			
			// 3) ZeroCross graphics
			if (AlertZeroCrossEnabled)
			{
			   if (  (Macd[i+1] < 0.0 && Macd[i] > 0)
			      || (Macd[i+1] > 0.0 && Macd[i] < 0) )
			      DoZeroCrossGraphic(Macd[i], i);
			
			}
			
			
			// ALERTS (NEW - OFF THE DOTS ON THE ZERO LINE. A COLOR OUT OF A BLACK IS AN ALERT
			if (AlertEnabled && i==1)
			{
				if (dotSeries[i+1] <= 0 && dotSeries[i] == +1)
				{
				  
					Comment("MACD Long Signal");
					Print("MACD Long Signal");
               if (AlertSoundEnabled)
                  PlaySound(AlertSound);
			   }
				else if (dotSeries[i+1] >= 0 && dotSeries[i] == -1)
				{
					Comment("MACD Short Signal");					
					Print("MACD Long Signal");
               if (AlertSoundEnabled)
                  PlaySound(AlertSound);
				}
			}
			
			
		// INSERTED TO MANUALLY COUNT IndicatorCountedMem ################################################################
       if (OfflineChart)
         IndicatorCountedMem = MathMax(2,Bars-i); // basically CurrentBar, although always looking back at least two bars
       	
        
       i--;
     }

   return(0);
  }
//+------------------------------------------------------------------+


// ************** SUPPORT FUNCTIONS ************************************

//+----------------------------------------------------------------------------------------------------------------------+





void DoObjectsDeleteAll()
{
      // custom iteration deleting all possible (existent or not) objects generated by this script
      
      
      // generic version based on short_name preface
      int    obj_total=ObjectsTotal();
      string name;
      int NameLength = StringLen(short_name);
      for (int x=0;x<obj_total;x++)
      {
         name=ObjectName(x);
         //Print(x,"Object name for object #",x," is " + name);
         if (StringSubstr(name,0,NameLength) == short_name)
         {
            ObjectDelete(name);
            x--;
         }
         
      }

      
      
      Comment("");   // remove any comment as well.
}

// ================================================================================

/*
string StringChangeToUpperCase(string sText) {
  // Example: StringChangeToUpperCase("oNe mAn"); // ONE MAN 
  int iLen=StringLen(sText), i, iChar;
  for(i=0; i < iLen; i++) {
    iChar=StringGetChar(sText, i);
    if(iChar >= 97 && iChar <= 122) sText=StringSetChar(sText, i, iChar-32);
  }
  return(sText);
}
 
*/


//+X================================================================X+
//| UpsizeSeriesArray() function                                     |
//+X================================================================X+
void UpsizeSeriesArray(double & Array[])
//----+
  {
  // this function adds a new empty bucket on the newest end of a series array. (DataSeries c# style.)
    if (ArraySize(Array)<Bars)
                          ArraySetAsSeries(Array, false);
    ArrayResize(Array, Bars);
    ArraySetAsSeries(Array, true);
  }
//----+


//+X================================================================X+
//| DoBandFill() function                                            |
//+X================================================================X+
// DoBandFill - simulates the MACD_Platinum on NinjaTrader's corduroy pattern.
void DoBandFill(int i)
{
   string name = short_name+":band"+(Bars-i);
   if (i <= 1)
      ObjectDelete(name);  // might be redrawing, so delete first.
      
   color col = MarkerColorUp;//MacdColor;
   
   
   //string graphmode = "C";
   if (Macd[i] < Avg[i])
   {
       col = MarkerColorDown;//AvgColor;
       //graphmode = "B";
   }
   
   
   int type = OBJ_TREND;
   if (Macd[i] != Avg[i])  
   {
      bool succeeded = ObjectCreate(name,type,Panel,Time[i],Macd[i],Time[i],Avg[i],NULL,NULL);
      ObjectSet(name,OBJPROP_COLOR,col);
      ObjectSet(name,OBJPROP_WIDTH,1);
      ObjectSet(name,OBJPROP_RAY, false);
   }
   
   /*
   //if (graphmode != "C")
   {
      string nameB = name+"B";
      succeeded = ObjectCreate(nameB,type,Panel,Time[i],Macd[i],Time[i+1],Avg[i+1],NULL,NULL);
      ObjectSet(nameB,OBJPROP_COLOR,col);
      ObjectSet(nameB,OBJPROP_WIDTH,1);
      ObjectSet(nameB,OBJPROP_RAY, false);
   }
   
  //if (graphmode != "B")
   {
      string nameC = name+"C";
      succeeded = ObjectCreate(nameC,type,Panel,Time[i+1],Macd[i+1],Time[i],Avg[i],NULL,NULL);
      ObjectSet(nameC,OBJPROP_COLOR,col);
      ObjectSet(nameC,OBJPROP_WIDTH,1);
      ObjectSet(nameC,OBJPROP_RAY, false);
   }
   */
   
}

// ====================================================================



//+X================================================================X+
//| ADXVMA() function                                                |
//+X================================================================X+		
	 double DoADXVMA( int period, int i)
		{
		 double WeightDM=Fast;
		 double WeightDI=Fast;
		 double WeightDX=Fast;
		 double ChandeEMA=Fast;
		 
			if( Bars - i < 2 )
			{
				ADXVMAPlot[i] =( 0 );
				PDM[i] = ( 0 );
				MDM[i] = ( 0 );
				PDI[i] = ( 0 );
				MDI[i] = ( 0 );
				Out[i] = ( 0 );
				return (0.0);
			}
			
			//
			{
				//int i = 0;
				PDM[i]=( 0 );
				MDM[i]=( 0 );
				if(Macd[i]>Macd[i+1])
					PDM[i]=( Macd[i]-Macd[i+1] );//This array is not displayed.
				else
					MDM[i]=( Macd[i+1]-Macd[i] );//This array is not displayed.
				
				PDM[i]=(((WeightDM-1)*PDM[i+1] + PDM[i])/WeightDM);//ema.
				MDM[i]=(((WeightDM-1)*MDM[i+1] + MDM[i])/WeightDM);//ema.
				
				double TR=PDM[i]+MDM[i];
				
				if (TR>0)
				{
					PDI[i]=(PDM[i]/TR);
					MDI[i]=(MDM[i]/TR);
				}//Avoid division by zero. Minimum step size is one unnormalized price pip.
				else
				{
					PDI[i]=(0);
					MDI[i]=(0);
				}
				
				PDI[i]=(((WeightDI-1)*PDI[i+1] + PDI[i])/WeightDI);//ema.
				MDI[i]=(((WeightDI-1)*MDI[i+1] + MDI[i])/WeightDI);//ema.

				double DI_Diff=PDI[i]-MDI[i];  
				if (DI_Diff<0)
					DI_Diff= -DI_Diff;//Only positive momentum signals are used.
				double DI_Sum=PDI[i]+MDI[i];
				double DI_Factor=0;//Zero case, DI_Diff will also be zero when DI_Sum is zero.
				if (DI_Sum>0)
					Out[i]=(DI_Diff/DI_Sum);//Factional, near zero when PDM==MDM (horizonal), near 1 for laddering.
				else
					Out[i]=(0);
	
				  Out[i]=(((WeightDX-1)*Out[i+1] + Out[i])/WeightDX);
				
				if (Out[i]>Out[i+1])
				{
					HHV=Out[i];
					LLV=Out[i+1];
				}
				else
				{
					HHV=Out[i+1];
					LLV=Out[i];
				}
	
				for(int j=1;j<MathMin(period,Bars-i);j++)
				{
					if(Out[i+j+1]>HHV)HHV=Out[i+j+1];
					if(Out[i+j+1]<LLV)LLV=Out[i+j+1];
				}
				
				
				double diff = HHV - LLV;//Veriable reference scale, adapts to recent activity level, unnormalized.
				double VI=0;//Zero case. This fixes the output at its historical level. 
				if (diff>0)
					VI=(Out[i]-LLV)/diff;//Normalized, 0-1 scale.
				
				//   if (VI_0.VIsq_1.VIsqroot_2==1)VI*=VI;
				//   if (VI_0.VIsq_1.VIsqroot_2==2)VI=MathSqrt(VI);
				//   if (VI>VImax)VI=VImax;//Used by Bemac with VImax=0.4, still used in vma1 and affects 5min trend definition.
				//   All the ema weight settings, including Chande, affect 5 min trend definition.
				//   if (VI<=zeroVIbelow)VI=0;                    
										
				ADXVMAPlot[i]=(((ChandeEMA-VI)*ADXVMAPlot[i+1]+VI*Macd[i])/ChandeEMA);//Chande VMA formula with ema built in.
			}
			
			
			return (ADXVMAPlot[i]);
        }
		
		// ------------------------------
		
//+X================================================================X+
//| DoDotSeries() function                                           |
//+X================================================================X+
// DoDotSeries - draws dots of different colors along the zero line
void DoDotSeries(int val, int i)
{
   string name = short_name+":dotZeries"+(Bars-i);
   string name2 = name+":2";
   if (i <= 1)
   {
      ObjectDelete(name);  // might be redrawing, so delete first.
      ObjectDelete(name2);
   }
      
   color col = Black;
   if (val < 0)
   {
       col = MarkerColorDown;
   }
   else if (val > 0)
   {
      col = MarkerColorUp;
   }
   
   double plotValue = 0.0;// + (WindowPriceMax(Panel)-WindowPriceMin(Panel)) / 50.0;
   int type = OBJ_ARROW;     
   /*bool succeeded = ObjectCreate(name,type,Panel,Time[i],plotValue,0,0,NULL,NULL);
   ObjectSet(name,OBJPROP_COLOR,col);
   ObjectSet(name,OBJPROP_WIDTH,0);
   ObjectSet(name,OBJPROP_ARROWCODE, 3);
   
   
   succeeded = ObjectCreate(name2,type,Panel,Time[i],plotValue,0,0,NULL,NULL);
   ObjectSet(name2,OBJPROP_COLOR,col);
   ObjectSet(name2,OBJPROP_WIDTH,8);
   ObjectSet(name2,OBJPROP_ARROWCODE, 4);*/
   
   
}

// ====================================================================


//+X================================================================X+
//| DoZeroCrossGraphic() function                                           |
//+X================================================================X+
// DoZeroCrossGraphic - draws dots 
void DoZeroCrossGraphic(double val, int i)
{
   string name = short_name+":crosses"+(Bars-i);
  
   if (i <= 1)
   {
      ObjectDelete(name);  // might be redrawing, so delete first.
   }
   int charf = 233;
   double recentActivity = 0;
   for (int a = i; a<MathMin(i+30,Bars); a++)
      recentActivity = MathMax(recentActivity,MathAbs(Macd[a]));
      
   //double graphDistance = (WindowPriceMax(Panel)-WindowPriceMin(Panel)) / 10.0;
   double graphDistance = recentActivity / 5.0;
   double plotValue = -graphDistance;
   if (val < 0.0)
   {
      charf = 234;
      plotValue = graphDistance*1.5;
   }
      
   color col = ZeroLineCrossColor;
   
    
   int type = OBJ_ARROW;     
   bool succeeded = ObjectCreate(name,type,Panel,Time[i],plotValue,0,0,NULL,NULL);
   ObjectSet(name,OBJPROP_COLOR,col);
   ObjectSet(name,OBJPROP_WIDTH,1);
   ObjectSet(name,OBJPROP_ARROWCODE, charf);

   
   
}

// ====================================================================