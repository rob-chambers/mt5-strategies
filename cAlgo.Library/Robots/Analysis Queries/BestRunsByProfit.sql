/* Select best runs by gross profit */
SELECT r.*, SUM(GrossProfit)
FROM [dbo].[Position] p INNER JOIN [dbo].[Run] r ON r.RunId = p.RunId
GROUP BY r.RunId, r.Symbol, r.Timeframe, r.TakeLongs, r.TakeShorts, r.CloseHalfAtBreakEven, r.CreatedDate, r.H4MAPeriod, r.InitialSLPips, r.InitialSLRule, r.LotSizingRule, r.MACrossRule, r.MACrossThreshold, r.MoveToBreakEven, r.PauseAfterPositionClosed, r.TakeProfitPips, r.TrailingSLPips, r.TrailingSLRule
HAVING SUM(GrossProfit) > 5000
ORDER BY SUM(GrossProfit) DESC