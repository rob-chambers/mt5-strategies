select top 20 r.RunId, sum(p.GrossProfit) AS GrossProfit, 
	CASE max(r.[InitialSLRule])
		WHEN 0 THEN 'None'
		WHEN 1 THEN 'CurrentBarNPips'
		WHEN 2 THEN 'PreviousBarNPips'
		WHEN 3 THEN 'ShortTermHighLow'
		WHEN 4 THEN 'StaticPipsValue'
	END AS InitialSLRule,
	max(r.[InitialSLPips]) AS InitialSLPips,
	CASE max(r.[TrailingSLRule])
		WHEN 0 THEN 'None'
		WHEN 1 THEN 'CurrentBarNPips'
		WHEN 2 THEN 'PreviousBarNPips'
		WHEN 3 THEN 'ShortTermHighLow'
		WHEN 4 THEN 'StaticPipsValue'
	END AS TrailingSLRule, 
	max(r.[TrailingSLPips]) AS TrailingSLPips,
	max(r.[TakeProfitPips]) AS TakeProfitPips,
	max(r.[PauseAfterPositionClosed]) AS PauseAfterPositionClosed,
	max(r.[MACrossThreshold]) AS MACrossThreshold,
	CASE max(r.[MACrossRule])
		WHEN 0 THEN 'None'
		WHEN 1 THEN 'CloseOnFastMaCross'
		WHEN 2 THEN 'CloseOnMediumMaCross'
	END AS MACrossRule
from dbo.run r inner join dbo.position p on r.runid = p.runid
group by r.RunId
order by sum(p.GrossProfit) desc