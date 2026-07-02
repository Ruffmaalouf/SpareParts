-- Minimal image update — no DDL, no PartImageEnrichment rows
-- Just UPDATE dbo.Parts for the 10 HIGH-confidence parts
-- Run via: sqlcmd -S localhost -E -d SparePartsDb -i minimal_update.sql -o minimal_output.txt
USE SparePartsDb;
GO

BEGIN TRANSACTION;

UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/49606/FBRB/S10075/S10075-01.jpg"]'                                                              WHERE Id = 1;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/7212/BCDP/7090/7090-01.jpg"]'                                                                   WHERE Id = 2;
UPDATE dbo.Parts SET ImageUrls = N'["https://brem-p-001.sitecorecontenthub.cloud/api/public/content/d4264abb1463428a86e81ef7a50e0dd4?v=2bb96c68"]'                                        WHERE Id = 3;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/6192/FBRB/SA10464/SA10464-01.jpg"]'                                                             WHERE Id = 4;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/7556/BCZC/5529/5529-01.jpg"]'                                                                   WHERE Id = 5;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/2412/BBHK/AL2352X/AL2352X-01.jpg"]'                                                             WHERE Id = 7;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/5132/BBHK/15479/15479-01.jpg"]'                                                                 WHERE Id = 10;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/2864/KFKP/HO2800155V/HO2800155V-01.jpg"]'                                                      WHERE Id = 11;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/2864/KFKP/HO2801155V/HO2801155V-01.jpg"]'                                                      WHERE Id = 12;
UPDATE dbo.Parts SET ImageUrls = N'["https://contentassets.autozone.com/product_image/USA/2864/KFKP/BM2818111/BM2818111-01.jpg"]'                                                        WHERE Id = 353;

COMMIT TRANSACTION;
GO

SELECT Id, LEFT(ImageUrls, 120) AS NewImageUrl
FROM   dbo.Parts
WHERE  Id IN (1,2,3,4,5,7,10,11,12,353)
ORDER  BY Id;
GO
PRINT 'Done — 10 parts updated.';
