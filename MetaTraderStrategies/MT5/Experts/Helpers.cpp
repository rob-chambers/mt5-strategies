// Code that may be useful in the future

bool DecentProfitSoFar()
{
    double largestProfit = _recentHigh - _position.PriceOpen();
    double initialRisk = _position.PriceOpen() - _initialStop;

    if (largestProfit > initialRisk * 0.75) {
        Print("Trade has made a decent profit so far");
        return true;
    }

    return false;
}

bool ProfitDroppedByHalf()
{
    double diff = _recentHigh - _currentAsk;
    double largestProfit = _recentHigh - _position.PriceOpen();

    if (diff > largestProfit / 2) {
        Print("Price dropped to below half profit");
        return true;
    }

    return false;
}


bool ShouldMoveShortToBreakEven(double newStop)
{
    if (_alreadyMovedToBreakEven) return false;

    double breakEvenPrice = _position.PriceOpen() * 2 - _initialStop;
    if (_currentBid < breakEvenPrice && (newStop == 0.0 || breakEvenPrice < newStop)) {
        printf("Moving to breakeven now that the price has reached %f", breakEvenPrice);

        /* ACTUALLY MOVE IT - Needs more thorough testing */
        // Changing this so we don't actually move the SL

        /* This has changed quite a bit recently.  Historically, we would always move the stop to breakeven.
        Then this was removed so we don't move the stop
        AND NOW...we move only if Martingale is active, meaning we have increased our risk beyond normal.
        This is a way to recover our losses quickly and manage the risk a little better.
        */
        //if (_martingaleActive) {
        //    //newStop = _position.PriceOpen();

        //    newStop = breakEvenPrice;
        //}

        return true;
    }

    return false;
}