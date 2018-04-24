//+------------------------------------------------------------------+
//|                                              WriteTradeHistory.mq5 
//|                                    Copyright 2018, Robert Chambers
//+------------------------------------------------------------------+
#property copyright     "Copyright 2018, Robert Chambers"
#property version       "1.10"
#property description   "Write Deal History from the last day"

/* Revision History

1.10:   * Added new input parameter to specify number of hours of history
*/
input int HoursOfHistory = 36; // Number of hours of history to write

int _fileHandle;

int OnInit() {
    int minutesInHour = 60;
    int minutes = HoursOfHistory * minutesInHour;
    datetime to = TimeCurrent();
    datetime from = to - 60 * minutes;

    if (!HistorySelect(from, to)) {
        Print("Failed to retrieve order history");
    }

    int dealsTotal = HistoryDealsTotal();
    if (dealsTotal <= 0) {
        Print("No deals found");
        return(INIT_SUCCEEDED);
    }

    Print("Found ", dealsTotal, " deals");

    MqlDateTime todayStruct;
    TimeToStruct(to, todayStruct);
   
    string fileName = IntegerToString(todayStruct.year, 2, '0') + "-" + IntegerToString(todayStruct.mon, 2, '0') + "-" + IntegerToString(todayStruct.day, 2, '0') + " TradeHistory.csv";
    Print(fileName);

    _fileHandle = FileOpen(fileName, FILE_WRITE | FILE_CSV);
    if (_fileHandle == INVALID_HANDLE) {
        Print("Error opening file for writing");
        return(INIT_FAILED);
    }

    for (int dealIndex = 0; dealIndex < dealsTotal; dealIndex++) {
        ulong inDeal = HistoryDealGetTicket(dealIndex);
        long dealNumber = HistoryDealGetInteger(inDeal, DEAL_TICKET);

        // type of entry
        long dealEntry = HistoryDealGetInteger(inDeal, DEAL_ENTRY);
        if (dealEntry != DEAL_ENTRY_IN) {
            continue;
        }

        string inSymbol = HistoryDealGetString(inDeal, DEAL_SYMBOL);
        datetime entryTime = (datetime)HistoryDealGetInteger(inDeal, DEAL_TIME);
        double volume = HistoryDealGetDouble(inDeal, DEAL_VOLUME);
        double entryPrice = HistoryDealGetDouble(inDeal, DEAL_PRICE);

        // Find the corresponding out deal
        bool foundExit = false;
        ulong outDeal;

        for (int outDealIndex = dealIndex + 1; outDealIndex < dealsTotal; outDealIndex++) {
            outDeal = HistoryDealGetTicket(outDealIndex);
            long exitDealNumber = HistoryDealGetInteger(outDeal, DEAL_TICKET);
            dealEntry = HistoryDealGetInteger(outDeal, DEAL_ENTRY);
            if (dealEntry == DEAL_ENTRY_OUT) {
                string outSymbol = HistoryDealGetString(outDeal, DEAL_SYMBOL);
                if (inSymbol == outSymbol) {
                    foundExit = true;
                    break;
                }
            }
        }

        datetime exitTime;
        double exitPrice;
        MqlDateTime entryTimeStruct, exitTimeStruct;
        double profit = 0;

        if (foundExit) {
            exitTime = (datetime)HistoryDealGetInteger(outDeal, DEAL_TIME);            
            TimeToStruct(exitTime, exitTimeStruct);

            exitPrice = HistoryDealGetDouble(outDeal, DEAL_PRICE);
            profit = HistoryDealGetDouble(outDeal, DEAL_PROFIT);
        }
        
        if (entryPrice && entryTime)
        {
            long dealType = HistoryDealGetInteger(inDeal, DEAL_TYPE);
            string dealTypeString;

            if (dealType == DEAL_TYPE_BUY) {
                dealTypeString = "L";
            }
            else if (dealType == DEAL_TYPE_SELL) {
                dealTypeString = "S";
            }

            double tp = 0;
            double sl = 0;

            string exitTimeString;
            string exitPriceString;

            if (foundExit) {
                exitTimeString = ToDate(exitTimeStruct);
                exitPriceString = (string)exitPrice;
            }

            TimeToStruct(entryTime, entryTimeStruct);

            string theSymbol;
            if (StringFind(inSymbol, "XA") > -1) {
                theSymbol = inSymbol;
            } else {
                theSymbol = StringSubstr(inSymbol, 0, 3) + "/" + StringSubstr(inSymbol, 3);
            }
            
            FileWrite(_fileHandle, ToDate(entryTimeStruct), theSymbol, dealTypeString, entryPrice, sl, tp, "", volume, exitTimeString, exitPriceString, "", profit);
        } else {
            Print("Didn't write to file");
        }        
    }

    FileClose(_fileHandle);
    
    return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
}

string ToDate(const MqlDateTime &value) {

    long seconds = TimeCurrent() - TimeLocal();
    double hours = (double)seconds / 3600;
    hours = MathCeil(hours);

    int correctHour = value.hour - (int)hours; // Adjust for platform time
    return IntegerToString(value.day, 2, '0') + "/" + IntegerToString(value.mon, 2, '0') + "/" + IntegerToString(value.year, 4, '0') + " " +
        IntegerToString(correctHour, 2, '0') + ":" + IntegerToString(value.min, 2, '0');
}