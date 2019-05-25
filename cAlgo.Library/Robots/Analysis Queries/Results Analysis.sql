/* H4 Analysis - results to go into sheet 'H4 Analysis 1' */

SELECT CASE 
	WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 'H4 above'
	WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 'H4 below 100'
	WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 'H4 below 60'
	WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 'H4 below 40'
	WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 'H4 below 20'
END AS H4Position,
SUM(CASE WHEN p.GrossProfit > 0 THEN 1 ELSE 0 END) AS Winners,
COUNT(*) AS TradeCount

FROM [dbo].[Position] p
WHERE p.TradeType = 'Buy' AND p.GrossProfit IS NOT NULL
GROUP BY CASE
	WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 'H4 above'
	WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 'H4 below 100'
	WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 'H4 below 60'
	WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 'H4 below 40'
	WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 'H4 below 20'
END

-- Find the top performing trades by profit - results to go into sheet 'Top Trades'
SELECT TOP 20 p.*
FROM [dbo].[Position] p
WHERE p.TradeType = 'Buy' AND p.GrossProfit > 1000
ORDER BY p.GrossProfit DESC

-- H4 Analysis - results to go into sheet 'H4 Analysis 2'
-- Perhaps we need to either close above the H4MA or be a little below, but not far below
SELECT 	
	CASE 
		WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 'H4 above'
		WHEN (p.[Close] - p.H4MA) * 10000 > 200 THEN 'H4 below 200'
		WHEN (p.[Close] - p.H4MA) * 10000 > 150 THEN 'H4 below 150'
		WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 'H4 below 100'
		WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 'H4 below 60'
		WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 'H4 below 40'
		WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 'H4 below 20'
	END AS H4BandName,	
	CASE 
		WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 0
		WHEN (p.[Close] - p.H4MA) * 10000 > 200 THEN 1
		WHEN (p.[Close] - p.H4MA) * 10000 > 150 THEN 2
		WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 3
		WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 4
		WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 5
		WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 6
	END AS H4BandIndex,
	count(*) AS Trades,
	SUM(p.GrossProfit) AS Profit,
	SUM(CASE WHEN ISNULL(p.GrossProfit, 0) > 0 THEN 1 ELSE 0 END) AS Winners,
	SUM(CASE WHEN ISNULL(p.GrossProfit, 0) <= 0 THEN 1 ELSE 0 END) AS Losers

FROM dbo.Position p
WHERE p.TradeType = 'Buy' AND p.GrossProfit IS NOT NULL
GROUP BY 
	case WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 'H4 above'
		WHEN (p.[Close] - p.H4MA) * 10000 > 200 THEN 'H4 below 200'
		WHEN (p.[Close] - p.H4MA) * 10000 > 150 THEN 'H4 below 150'
		WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 'H4 below 100'
		WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 'H4 below 60'
		WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 'H4 below 40'
		WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 'H4 below 20'
	end,
	CASE 
			WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 0
			WHEN (p.[Close] - p.H4MA) * 10000 > 200 THEN 1
			WHEN (p.[Close] - p.H4MA) * 10000 > 150 THEN 2
			WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 3
			WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 4
			WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 5
			WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 6
	END
ORDER BY
	CASE 
		WHEN (p.[Close] - p.H4MA) * 10000 < 0 THEN 0
		WHEN (p.[Close] - p.H4MA) * 10000 > 200 THEN 1
		WHEN (p.[Close] - p.H4MA) * 10000 > 150 THEN 2
		WHEN (p.[Close] - p.H4MA) * 10000 > 100 THEN 3
		WHEN (p.[Close] - p.H4MA) * 10000 > 60 THEN 4
		WHEN (p.[Close] - p.H4MA) * 10000 > 40 THEN 5
		WHEN (p.[Close] - p.H4MA) * 10000 > 20 THEN 6
	END



-- RSI Analysis - results to go into sheet 'RSI Analysis'
SELECT 	
	CASE 
		WHEN p.RSI < 50 THEN 'Below 50'
		WHEN p.RSI < 55 THEN 'Below 55'
		WHEN p.RSI < 60 THEN 'Below 60'
		WHEN p.RSI < 65 THEN 'Below 65'
		WHEN p.RSI < 70 THEN 'Below 70'
		WHEN p.RSI < 75 THEN 'Below 75'
		ELSE 'Above 75'
	END AS RSIBand,	
	CASE 
		WHEN p.RSI < 50 THEN 0
		WHEN p.RSI < 55 THEN 1
		WHEN p.RSI < 60 THEN 2
		WHEN p.RSI < 65 THEN 3
		WHEN p.RSI < 70 THEN 4
		WHEN p.RSI < 75 THEN 5
		ELSE 6
	END,
	COUNT(*) AS Trades,
	SUM(CASE WHEN ISNULL(p.GrossProfit, 0) > 0 THEN 1 ELSE 0 END) AS Winners,
	SUM(CASE WHEN ISNULL(p.GrossProfit, 0) <= 0 THEN 1 ELSE 0 END) AS Losers

FROM dbo.Position p
WHERE p.TradeType = 'Buy' AND GrossProfit is NOT NULL
GROUP BY
	CASE 
		WHEN p.RSI < 50 THEN 'Below 50'
		WHEN p.RSI < 55 THEN 'Below 55'
		WHEN p.RSI < 60 THEN 'Below 60'
		WHEN p.RSI < 65 THEN 'Below 65'
		WHEN p.RSI < 70 THEN 'Below 70'
		WHEN p.RSI < 75 THEN 'Below 75'
		ELSE 'Above 75'
	END,
	CASE 
		WHEN p.RSI < 50 THEN 0
		WHEN p.RSI < 55 THEN 1
		WHEN p.RSI < 60 THEN 2
		WHEN p.RSI < 65 THEN 3
		WHEN p.RSI < 70 THEN 4
		WHEN p.RSI < 75 THEN 5
		ELSE 6
	END

ORDER BY 
CASE 
		WHEN p.RSI < 50 THEN 0
		WHEN p.RSI < 55 THEN 1
		WHEN p.RSI < 60 THEN 2
		WHEN p.RSI < 65 THEN 3
		WHEN p.RSI < 70 THEN 4
		WHEN p.RSI < 75 THEN 5
		ELSE 6
	END


-- Range Analysis - results to go into sheet 'Range Analysis'
SELECT
CASE 
	WHEN (p.High - p.Low) > 0.02 THEN '200'
	WHEN (p.High - p.Low) > 0.01 THEN '100'
	WHEN (p.High - p.Low) > 0.0075 THEN '75'
	WHEN (p.High - p.Low) > 0.005 THEN '50'
	WHEN (p.High - p.Low) > 0.0025 THEN '25'
	WHEN (p.High - p.Low) > 0.0015 THEN '15'
	ELSE 'Minimal'
END,
COUNT(*) AS Trades,
SUM(CASE WHEN ISNULL(p.GrossProfit, 0) > 0 THEN 1 ELSE 0 END) AS Winners,
SUM(CASE WHEN ISNULL(p.GrossProfit, 0) <= 0 THEN 1 ELSE 0 END) AS Losers

FROM dbo.[Position] p
WHERE p.GrossProfit IS NOT NULL
GROUP BY 
CASE 
	WHEN (p.High - p.Low) > 0.02 THEN '200'
	WHEN (p.High - p.Low) > 0.01 THEN '100'
	WHEN (p.High - p.Low) > 0.0075 THEN '75'
	WHEN (p.High - p.Low) > 0.005 THEN '50'
	WHEN (p.High - p.Low) > 0.0025 THEN '25'
	WHEN (p.High - p.Low) > 0.0015 THEN '15'
	ELSE 'Minimal'
END


-- Close off High Analysis - results to go into sheet 'Close off High Analysis'
-- Where did we close in relation to the high?
SELECT
CASE 
	WHEN (p.High - p.[Close]) < 0.0005 THEN '5'
	WHEN (p.High - p.[Close]) < 0.0010 THEN '10'
	WHEN (p.High - p.[Close]) < 0.0015 THEN '15'
	WHEN (p.High - p.[Close]) < 0.0020 THEN '20'
	WHEN (p.High - p.[Close]) < 0.0025 THEN '25'
	WHEN (p.High - p.[Close]) < 0.0030 THEN '30'
	ELSE 'Large Difference'
END,
COUNT(*) AS Trades,
SUM(CASE WHEN ISNULL(p.GrossProfit, 0) > 0 THEN 1 ELSE 0 END) AS Winners,
SUM(CASE WHEN ISNULL(p.GrossProfit, 0) <= 0 THEN 1 ELSE 0 END) AS Losers

FROM dbo.[Position] p
WHERE p.GrossProfit IS NOT NULL
GROUP BY 
CASE 
	WHEN (p.High - p.[Close]) < 0.0005 THEN '5'
	WHEN (p.High - p.[Close]) < 0.0010 THEN '10'
	WHEN (p.High - p.[Close]) < 0.0015 THEN '15'
	WHEN (p.High - p.[Close]) < 0.0020 THEN '20'
	WHEN (p.High - p.[Close]) < 0.0025 THEN '25'
	WHEN (p.High - p.[Close]) < 0.0030 THEN '30'
	ELSE 'Large Difference'
END