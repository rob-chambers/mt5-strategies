namespace Powder.TradingLibrary
{
    public enum TrailingStopLossRuleValues
    {
        None,
        CurrentBarNPips,
        PreviousBarNPips,
        ShortTermHighLow,
        StaticPipsValue,
        SmartProfitLocker,
        OppositeColourBar
    };
}
