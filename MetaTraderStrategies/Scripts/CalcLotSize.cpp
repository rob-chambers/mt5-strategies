//+------------------------------------------------------------------+
//|                                                    CalcLotSize.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright     "Copyright 2018, Robert Chambers"
#property version       "1.00"
#property description   "Lot Size Calculator"

#include <Trade\SymbolInfo.mqh> 
#include <ChartObjects\ChartObjectsTxtControls.mqh>

input double Risk = 0.2; // Risk per trade as percentage of account size (e.g. 1 for 1%)

CSymbolInfo _symbol;
CChartObjectEdit _inputStopPrice;
CChartObjectLabel _lotSizeLabel;
bool _textBoxCreated;

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

    _textBoxCreated = _inputStopPrice.Create(0, "_inputStopPrice", 0, 800, 10, 80, 25);
    if (_textBoxCreated) {
        _inputStopPrice.BackColor(White);
        _inputStopPrice.BorderColor(Black);

        _lotSizeLabel.Create(0, "_lotSizeLabel", 0, 800, 43);
        _lotSizeLabel.SetString(OBJPROP_TEXT, "Enter a stop price");
        _lotSizeLabel.Color(Green);

        string defaultText = DoubleToString(_symbol.Ask(), 2);
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
    }
}

double ComputeRisk(double distance)
{
    double accountBalance, riskInMoney;
    double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
    double tickValue = SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_VALUE);
    double tickSize = SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_SIZE);

    int lotdigits = 0;
    do
    {
        lotdigits++;
        lotStep *= 10;
    } while (lotStep < 1);

    accountBalance = MathMin(AccountInfoDouble(ACCOUNT_BALANCE), AccountInfoDouble(ACCOUNT_EQUITY));
    riskInMoney = accountBalance * Risk / 100;

    double lot = NormalizeDouble(riskInMoney / (distance * (tickValue / tickSize)), lotdigits);

    // Hack for JPY pairs
    if (_symbol.Digits() == 3) {
        lot = lot / 10;
    }

    lot = MathFloor(lot * MathPow(10, lotdigits)) / MathPow(10, lotdigits);

    return NormalizeDouble(lot, lotdigits);
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
            double price = _symbol.Ask();
            double stopLoss = StringToDouble(_inputStopPrice.GetString(OBJPROP_TEXT));
            double sl = NormalizeDouble(stopLoss, _Digits);
            double lot = ComputeRisk(MathAbs(price - sl));

            message = "Lot size should be: " + (string)lot;
        }

        _lotSizeLabel.SetString(OBJPROP_TEXT, message);
        ChartRedraw(0);
    }
}