//+------------------------------------------------------------------+
//|                                                   CalcLotSize2.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright     "Copyright 2018, Robert Chambers"
#property version       "1.10"
#property description   "Lot Size Calculator"

/* Revision History 

1.10:   * Fix lot size - reduce to proper risk percentage amount by using existing MoneyFixedRisk class.
        * Removed print statements used for debugging
        * 
*/


#include <Trade\SymbolInfo.mqh> 
#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <Expert\Money\MoneyFixedRisk.mqh>

input double Risk = 1;              // Risk per trade as percentage of account size (e.g. 1 for 1%)
input int PipsFromSignalCandle = 2; // Number of pips the default stop loss should be from the signal candle

CSymbolInfo _symbol;
CChartObjectEdit _inputStopPrice;
CMoneyFixedRisk _fixedRisk;

CChartObjectLabel _lotSizeLabel;
bool _textBoxCreated;
int _qmpFilterHandle;
double _qmpFilterUpData[];
double _qmpFilterDownData[];

int OnInit() {
    if (!_symbol.Name(_Symbol)) {
        Print("Error setting symbol name");
        return(INIT_FAILED);
    }

    if (!_symbol.RefreshRates()) {
        Print("Error refreshing rates!");
        return(INIT_FAILED);
    }

    if (_symbol.Ask() == 0 || _symbol.Bid() == 0) {
        MessageBox("Couldn't get rates");
        return(INIT_FAILED);
    }

    if (!_fixedRisk.Init(&_symbol, PERIOD_CURRENT, 1)) {
        Print("Couldn't initialise fixed risk instance");
        return(INIT_FAILED);
    }

    _fixedRisk.Percent(Risk);

    _textBoxCreated = _inputStopPrice.Create(0, "_inputStopPrice", 0, 800, 10, 90, 28);

    if (_textBoxCreated) {
        _inputStopPrice.BackColor(White);
        _inputStopPrice.BorderColor(Black);

        _lotSizeLabel.Create(0, "_lotSizeLabel", 0, 800, 43);
        _lotSizeLabel.SetString(OBJPROP_TEXT, "Enter a stop price");
        _lotSizeLabel.Color(Green);

        _qmpFilterHandle = iCustom(_Symbol, PERIOD_CURRENT, "QMP Filter", PERIOD_CURRENT, 12, 26, 9, true, 1, 8, 3, false, false);
        if (_qmpFilterHandle == INVALID_HANDLE) {
            Print("Error creating QMP Filter indicator");
        }

        ArraySetAsSeries(_qmpFilterUpData, true);
        ArraySetAsSeries(_qmpFilterDownData, true);

        int count = CopyBuffer(_qmpFilterHandle, 0, 0, 3, _qmpFilterUpData);
        if (count == -1) {
            Print("Error copying QMP Filter data for up buffer.");
            return(INIT_FAILED);
        }

        count = CopyBuffer(_qmpFilterHandle, 1, 0, 3, _qmpFilterDownData);
        if (count == -1) {
            Print("Error copying QMP Filter data for down buffer.");
            return(INIT_FAILED);
        }

        double size = 0.0001;
        if (_symbol.Digits() == 3) {
            size *= 100;
        }

        double initialStop = 0;
        double distance = PipsFromSignalCandle * size;        

        for (int index = 1; index <= 2; index++) {
            if (_qmpFilterUpData[index] != 0.0 && MathAbs(_qmpFilterUpData[index]) < 10000) {
                initialStop = _qmpFilterUpData[index] - distance;
                break;
            }
            else if (_qmpFilterDownData[index] != 0.0 && MathAbs(_qmpFilterDownData[index]) < 10000) {
                initialStop = _qmpFilterDownData[index] + distance;
                break;
            }
        }

        string defaultText = DoubleToString(initialStop, 5);
        _inputStopPrice.SetString(OBJPROP_TEXT, defaultText);
        _inputStopPrice.SetInteger(OBJPROP_SELECTED, true);
        
        ChartRedraw(0);
    }

    return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
    if (_textBoxCreated) {
        _inputStopPrice.Delete();
        _lotSizeLabel.Delete();

        ReleaseIndicator(_qmpFilterHandle);
    }
}

void ReleaseIndicator(int& handle) {
    if (handle != INVALID_HANDLE && IndicatorRelease(handle)) {
        handle = INVALID_HANDLE;
    }
    else {
        Print("IndicatorRelease() failed. Error ", GetLastError());
    }
}

//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(
    const int id,
    const long &lparam,
    const double &dparam,
    const string &sparam)
{
    if (id == CHARTEVENT_OBJECT_ENDEDIT && sparam == "_inputStopPrice")
    {
        string message;

        if (!_symbol.RefreshRates()) {
            message = "Couldn't get latest prices.";
        }
        else {            
            double stopLoss = StringToDouble(_inputStopPrice.GetString(OBJPROP_TEXT));
            double price = _symbol.Ask();
            if (stopLoss > price) {
                price = _symbol.Bid();
            }

            double sl = NormalizeDouble(stopLoss, _Digits);
            double lotSize;
            if (sl < price) {
                lotSize = _fixedRisk.CheckOpenLong(price, sl);
            }
            else {
                lotSize = _fixedRisk.CheckOpenShort(price, sl);
            }

            message = "Lot size should be: " + (string)lotSize;
        }

        _lotSizeLabel.SetString(OBJPROP_TEXT, message);
        ChartRedraw(0);
    }
}