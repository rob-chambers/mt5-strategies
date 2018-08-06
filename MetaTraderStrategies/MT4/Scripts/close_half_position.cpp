//+------------------------------------------------------------------+
//|                                                        close.mq4 |
//|                      Copyright © 2004, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2004, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net/"

//+------------------------------------------------------------------+
//| script "close first market order if it is first in the list"     |
//+------------------------------------------------------------------+
int start()
{       
    if (OrderSelect(0,SELECT_BY_POS,MODE_TRADES)) {
        double minLot = MarketInfo(Symbol(), MODE_MINLOT);
        double lotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
        double Lots = OrderLots();
        double half_close = MathFloor(Lots / 2 / lotStep) * lotStep;
        int orderType = OrderType();
        double price;

        //---- first order is buy or sell
        if (orderType == OP_BUY || orderType == OP_SELL) {
            if (orderType == OP_BUY) {
                price = Bid;
            }
            else {
                price = Ask;
            }

            bool result = OrderClose(OrderTicket(), half_close, price, 3, CLR_NONE);
        }        
    }
    else {
        Print("Error when performing OrderSelect ", GetLastError(), "  half position " + (string)half_close);
    }

    return 0;
}