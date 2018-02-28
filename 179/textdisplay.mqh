//=====================================================================
//	A library for output of text information on the chart.
//=====================================================================

//---------------------------------------------------------------------
#property copyright 	"Dima S., 2010 г."
#property link      	"dimascub@mail.com"
//---------------------------------------------------------------------

//---------------------------------------------------------------------
#import		"user32.dll"
int      GetSystemMetrics(int _index);
#import
//---------------------------------------------------------------------
#define		SM_CXSCREEN				0
#define		SM_CYSCREEN				1
#define		SM_CXFULLSCREEN		16
#define		SM_CYFULLSCREEN		17
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Include libraries
//---------------------------------------------------------------------
#include	<ChartObjects\ChartObjectsTxtControls.mqh>
#include	<Arrays\List.mqh>
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	TTitleDisplay class
//---------------------------------------------------------------------
class TTitleDisplay : public CChartObjectLabel
  {
protected:
   long              chart_id;
   int               sub_window;
   long              chart_width;                        // chart width in pixels
   long              chart_height;                       // chart height in pixels
   long              chart_width_step;                   // chart width step
   long              chart_height_step;                  // chart height step
   int               columns_number;                     // number of columns
   int               lines_number;                       // number of rows
   int               curr_column;
   int               curr_row;

private:
   void              SetParams(long _chart_id,int _window,int _cols,int _lines);// set object parameters

public:
   string            GetUniqName();                      // get unique name
   bool              Create(long _chart_id,int _window,int _cols,int _lines,int _col,int _row);
   void              RecalcAndRedraw();                  // recalculate coordinates and redraw

public:
   void              TTitleDisplay();                    // constructor
   void             ~TTitleDisplay();                    // destructor

  };
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Constructor
//---------------------------------------------------------------------
void TTitleDisplay::TTitleDisplay()
  {
  }
//---------------------------------------------------------------------
//	Desctructor
//---------------------------------------------------------------------
void TTitleDisplay::~TTitleDisplay()
  {
  }
//---------------------------------------------------------------------
//	Create object
//---------------------------------------------------------------------
bool TTitleDisplay::Create(long _chart_id,int _window,int _cols,int _lines,int _col,int _row)
  {
   this.curr_column=_col;
   this.curr_row=_row;
   SetParams(_chart_id,_window,_cols,_lines);

   return(this.Create(this.chart_id,this.GetUniqName(),this.sub_window,(int)(_col*this.chart_width_step),(int)(_row*this.chart_height_step)));
  }
//---------------------------------------------------------------------
//	Set object parameters
//---------------------------------------------------------------------
void TTitleDisplay::SetParams(long _chart_id,int _window,int _cols,int _lines)
  {
   this.chart_id=_chart_id;
   this.sub_window=_window;
   this.columns_number=_cols;
   this.lines_number=_lines;

//	get chart width in pixels
   this.chart_width=GetSystemMetrics(SM_CXFULLSCREEN);
   this.chart_height=GetSystemMetrics(SM_CYFULLSCREEN);

//	calculate steps
   this.chart_width_step=this.chart_width/_cols;
   this.chart_height_step=this.chart_height/_lines;
  }
//---------------------------------------------------------------------
//	Recalculate object parameters and redraw
//---------------------------------------------------------------------
void TTitleDisplay::RecalcAndRedraw()
  {
//	Get size of the chart (in pixels)
   long   width=GetSystemMetrics(SM_CXFULLSCREEN);
   long   height=GetSystemMetrics(SM_CYFULLSCREEN);
   if(width==this.chart_width && height==this.chart_height)
     {
      return;
     }

   this.chart_width=width;
   this.chart_height=height;

//	Recalculate steps
   this.chart_width_step=this.chart_width/this.columns_number;
   this.chart_height_step=this.chart_height/this.lines_number;

//	Move object to new coordinates
   this.X_Distance(( int )( this.curr_column * this.chart_width_step ));
   this.Y_Distance(( int )( this.curr_row * this.chart_height_step ));
  }
//---------------------------------------------------------------------
//	Get unique name
//---------------------------------------------------------------------
string TTitleDisplay::GetUniqName()
  {
   static uint   prev_count=0;

   uint         count=GetTickCount();
   while(1)
     {
      if(prev_count==UINT_MAX)
        {
         prev_count=0;
        }
      if(count<=prev_count)
        {
         prev_count++;
         count=prev_count;
        }
      else
        {
         prev_count=count;
        }

      //	Check the presence of the object with the same name
      string      name=TimeToString(TimeGMT(),TIME_DATE|TIME_MINUTES|TIME_SECONDS)+" "+DoubleToString(count,0);
      if(ObjectFind(0,name)<0)
        {
         return(name);
        }
     }

   return(NULL);
  }
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	TFieldDisplay class
//---------------------------------------------------------------------
class TFieldDisplay : public CChartObjectEdit
  {
protected:
   long              chart_id;
   int               sub_window;
   long              chart_width;                    // chart width in pixels
   long              chart_height;                   // chart height in pixels
   long              chart_width_step;               // horizontal step size
   long              chart_height_step;              // vertical step size
   int               columns_number;                 // number of columns
   int               limes_number;                   // number of rows
   int               curr_column;
   int               curr_row;

private:
   int               type;                           // edit field type ( string, numerical )

private:
   void              SetParams(long _chart_id,int _window,int _cols,int _lines);// set object parameters

public:
   string            GetUniqName();                  // get unique name
   bool              Create(long _chart_id,int _window,int _cols,int _lines,int _col,int _row);
   void              RecalcAndRedraw();              // recalculate coordinates and redraw

public:
   void              TFieldDisplay();                // constructor
   void             ~TFieldDisplay();                // desctructor
  };
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Constructor
//---------------------------------------------------------------------
void TFieldDisplay::TFieldDisplay()
  {
  }
//---------------------------------------------------------------------
//	Destructor
//---------------------------------------------------------------------
void TFieldDisplay::~TFieldDisplay()
  {
  }
//---------------------------------------------------------------------
//	Create object
//---------------------------------------------------------------------
bool TFieldDisplay::Create(long _chart_id,int _window,int _cols,int _lines,int _col,int _row)
  {
   this.curr_column=_col;
   this.curr_row=_row;
   SetParams(_chart_id,_window,_cols,_lines);

   return(this.Create(this.chart_id,this.GetUniqName(),this.sub_window,(int)(_col*this.chart_width_step),(int)(_row*this.chart_height_step)));
  }
//---------------------------------------------------------------------
//	Set object parameters
//---------------------------------------------------------------------
void TFieldDisplay::SetParams(long _chart_id,int _window,int _cols,int _lines)
  {
   this.chart_id=_chart_id;
   this.sub_window=_window;
   this.columns_number=_cols;
   this.limes_number=_lines;

//	get window width in pixels
   this.chart_width=GetSystemMetrics(SM_CXFULLSCREEN);
   this.chart_height=GetSystemMetrics(SM_CYFULLSCREEN);

//	calculate steps
   this.chart_width_step=this.chart_width/_cols;
   this.chart_height_step=this.chart_height/_lines;
  }
//---------------------------------------------------------------------
//	Recalculate and redraw
//---------------------------------------------------------------------
void TFieldDisplay::RecalcAndRedraw()
  {
//	get window size (in pixels)
   long   width=GetSystemMetrics(SM_CXFULLSCREEN);
   long   height=GetSystemMetrics(SM_CYFULLSCREEN);
   if(width==this.chart_width && height==this.chart_height)
     {
      return;
     }

   this.chart_width=width;
   this.chart_height=height;

//	Calculate steps
   this.chart_width_step=this.chart_width/this.columns_number;
   this.chart_height_step=this.chart_height/this.limes_number;

//	Move object to new coordinates
   this.X_Distance(( int )( this.curr_column * this.chart_width_step ));
   this.Y_Distance(( int )( this.curr_row * this.chart_height_step ));
  }
//---------------------------------------------------------------------
//	Get unique name
//---------------------------------------------------------------------
string TFieldDisplay::GetUniqName()
  {
   static uint   prev_count=0;

   uint         count=GetTickCount();
   while(1)
     {
      if(prev_count==UINT_MAX)
        {
         prev_count=0;
        }
      if(count<=prev_count)
        {
         prev_count++;
         count=prev_count;
        }
      else
        {
         prev_count=count;
        }

      //	Проверим, нет ли уже объекта с таким именем:
      string      name=TimeToString(TimeGMT(),TIME_DATE|TIME_MINUTES|TIME_SECONDS)+" "+DoubleToString(count,0);
      if(ObjectFind(0,name)<0)
        {
         return(name);
        }
     }

   return(NULL);
  }
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	TableDisplay class (List of objects)
//---------------------------------------------------------------------
class TableDisplay : public CList
  {
protected:
   long              chart_id;
   int               sub_window;
   ENUM_BASE_CORNER  corner;

public:
   void              SetParams(long _chart_id,int _window,ENUM_BASE_CORNER _corner=CORNER_LEFT_UPPER);
   int               AddTitleObject(int _cols,int _lines,int _col,int _row,string _title,color _color,string _fontname="Arial",int _fontsize=10);
   int               AddFieldObject(int _cols,int _lines,int _col,int _row,color _color,string _fontname="Arial",int _fontsize=10);
   bool              SetColor(int _index,color _color);
   bool              SetFont(int _index,string _fontname,int _fontsize);
   bool              SetText(int _index,string _text);
   bool              SetAnchor(int _index,ENUM_ANCHOR_POINT _anchor);

public:
   void              TableDisplay();
   void             ~TableDisplay();
  };
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Constructor
//---------------------------------------------------------------------
void TableDisplay::TableDisplay()
  {
   this.chart_id=0;
   this.sub_window=0;
   this.corner=CORNER_LEFT_UPPER;
  }
//---------------------------------------------------------------------
//	Destructor
//---------------------------------------------------------------------
void TableDisplay::~TableDisplay()
  {
//	Delete all objects
   this.Clear();
  }
//---------------------------------------------------------------------
//	Set common parameters for all list objects
//---------------------------------------------------------------------
void TableDisplay::SetParams(long _chart_id,int _window,ENUM_BASE_CORNER _corner)
  {
   this.chart_id=_chart_id;
   this.sub_window=_window;
   this.corner=_corner;
  }
//---------------------------------------------------------------------
//	Add a header (title) object
//---------------------------------------------------------------------
int TableDisplay::AddTitleObject(int _cols,int _lines,int _col,int _row,string _title,color _color,string _fontname,int _fontsize)
  {
   TTitleDisplay      *title=new TTitleDisplay();
   title.Create( this.chart_id, this.sub_window, _cols, _lines, _col, _row );
   title.Description( _title );
   title.Color( _color );
   title.Font( _fontname );
   title.FontSize( _fontsize );
   title.Corner( this.corner );
   return(this.Add(title));
  }
//---------------------------------------------------------------------
//	Add Edit Field object 
//---------------------------------------------------------------------
int TableDisplay::AddFieldObject(int _cols,int _lines,int _col,int _row,color _color,string _fontname,int _fontsize)
  {
   TFieldDisplay      *field=new TFieldDisplay();
   field.Create( this.chart_id, this.sub_window, _cols, _lines, _col, _row );
   field.Description( "" );
   field.Color( _color );
   field.Font( _fontname );
   field.FontSize( _fontsize );
   field.Corner( this.corner );
   return(this.Add(field));
  }
//---------------------------------------------------------------------
//	Set Anchor points
//---------------------------------------------------------------------
bool TableDisplay::SetAnchor(int _index,ENUM_ANCHOR_POINT _anchor)
  {
   CChartObjectText   *object=GetNodeAtIndex(_index);
   if(object==NULL)
     {
      return(false);
     }
   return(object.Anchor(_anchor));
  }
//---------------------------------------------------------------------
//	Set color of graphic object
//---------------------------------------------------------------------
bool TableDisplay::SetColor(int _index,color _color)
  {
   CChartObjectText   *object=GetNodeAtIndex(_index);
   if(object==NULL)
     {
      return(false);
     }
   return(object.Color(_color));
  }
//---------------------------------------------------------------------
//	Set font
//---------------------------------------------------------------------
bool TableDisplay::SetFont(int _index,string _fontname,int _fontsize)
  {
   CChartObjectText   *object=GetNodeAtIndex(_index);
   if(object==NULL)
     {
      return(false);
     }

   if(object.Font(_fontname)==false)
     {
      return(false);
     }
   return(object.FontSize(_fontsize));
  }
//---------------------------------------------------------------------
//	Set text
//---------------------------------------------------------------------
bool TableDisplay::SetText(int _index,string _text)
  {
   CChartObjectText   *object=GetNodeAtIndex(_index);
   if(object==NULL)
     {
      return(false);
     }
   return(object.Description(_text));
  }
//---------------------------------------------------------------------
