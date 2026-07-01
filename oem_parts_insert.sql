-- ============================================================
-- OEM PARTS INSERT SCRIPT — All 33 Donor Vehicles
-- CategoryIds: 1=Engine 2=Transmission 3=Brakes 4=Suspension
--   5=Electrical 6=Body & Exterior 7=Filters 8=Oils & Fluids
--   20=Cooling & Heating 22=Drivetrain 23=Exhaust System
--   24=Fuel System 27=Lighting 28=Safety 29=Steering 30=Wheels & Tires
-- Condition: 2=Used  PricingStatus: Manual
-- ============================================================
SET NOCOUNT ON;

-- ============================================================
-- VEHICLE 1: BMW 335i 2010 — N54 Twin-Turbo Inline-6
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11427953125',N'BMW 335i 2010 Engine Oil Filter Kit',N'11427953125',2,7,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 oil filter kit. Verify before selling.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11427953125');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-13717536006',N'BMW 335i 2010 Engine Air Filter',N'13717536006',2,7,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-13717536006');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-64316946674',N'BMW 335i 2010 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-12122158253',N'BMW 335i 2010 Iridium Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 iridium spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11537585907',N'BMW 335i 2010 Engine Thermostat',N'11537585907',2,20,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 thermostat with housing.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11537585907');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11517583787',N'BMW 335i 2010 Electric Water Pump',N'11517583787',2,20,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 electric water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11517583787');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-13537585261',N'BMW 335i 2010 High-Pressure Fuel Injector',N'13537585261',2,24,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 HPFP fuel injector.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-13537585261');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11367585425',N'BMW 335i 2010 VANOS Intake Solenoid',N'11367585425',2,5,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 intake VANOS solenoid valve.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11367585425');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11127565284',N'BMW 335i 2010 Valve Cover Gasket Left',N'11127565284',2,1,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 left valve cover gasket.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11127565284');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11317512081',N'BMW 335i 2010 Timing Chain Tensioner',N'11317512081',2,1,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 timing chain tensioner.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11317512081');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11280417143',N'BMW 335i 2010 Belt Tensioner',N'11280417143',2,1,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 belt tensioner pulley.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11280417143');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-17137534065',N'BMW 335i 2010 Coolant Expansion Tank',N'17137534065',2,20,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW coolant expansion tank.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-17137534065');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-34116776019',N'BMW 335i 2010 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E90/E92.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-34216764655',N'BMW 335i 2010 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E90/E92.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-34116792217',N'BMW 335i 2010 Front Brake Rotor',N'34116792217',2,3,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake disc rotor E90/E92.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-34116792217');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11427508968',N'BMW 335i 2010 Oil Filter Housing Gasket',N'11427508968',2,1,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 oil filter housing gasket.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11427508968');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-11787566643',N'BMW 335i 2010 Oxygen Sensor',N'11787566643',2,5,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 pre-cat oxygen sensor.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-11787566643');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0001-OEM-12137592232',N'BMW 335i 2010 Ignition Coil',N'12137592232',2,5,0,0,N'USD',0,1,1,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 ignition coil (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0001-OEM-12137592232');

-- ============================================================
-- VEHICLE 2: BMW 650i 2009 — N62 V8 Naturally Aspirated
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-11427509430',N'BMW 650i 2009 Engine Oil Filter Kit',N'11427509430',2,7,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-11427509430');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-13720427637',N'BMW 650i 2009 Engine Air Filter',N'13720427637',2,7,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 V8 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-13720427637');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-64316946674',N'BMW 650i 2009 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-12120037244',N'BMW 650i 2009 Spark Plug N62 V8',N'12120037244',2,1,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 spark plug (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-12120037244');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-11537509227',N'BMW 650i 2009 Engine Thermostat',N'11537509227',2,20,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 thermostat housing.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-11537509227');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-11517527799',N'BMW 650i 2009 Water Pump',N'11517527799',2,20,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-11517527799');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-12137592232',N'BMW 650i 2009 Ignition Coil',N'12137592232',2,5,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 ignition coil (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-12137592232');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-34116776019',N'BMW 650i 2009 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E63/E64.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-34216764655',N'BMW 650i 2009 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E63/E64.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-17137534065',N'BMW 650i 2009 Coolant Expansion Tank',N'17137534065',2,20,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW coolant expansion tank.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-17137534065');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0002-OEM-11141437929',N'BMW 650i 2009 Valley Pan Gasket N62',N'11141437929',2,1,0,0,N'USD',0,1,2,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 valley pan gasket (upper engine cover).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0002-OEM-11141437929');

-- ============================================================
-- VEHICLE 3: BMW X5 4.8 2008 — N62 V8 (E70)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-11427509430',N'BMW X5 4.8 2008 Engine Oil Filter Kit',N'11427509430',2,7,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-11427509430');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-13720427637',N'BMW X5 4.8 2008 Engine Air Filter',N'13720427637',2,7,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-13720427637');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-64316946674',N'BMW X5 4.8 2008 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-12120037244',N'BMW X5 4.8 2008 Spark Plug N62 V8',N'12120037244',2,1,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 spark plug (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-12120037244');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-11537509227',N'BMW X5 4.8 2008 Engine Thermostat',N'11537509227',2,20,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-11537509227');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-11517527799',N'BMW X5 4.8 2008 Water Pump',N'11517527799',2,20,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-11517527799');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-34116776019',N'BMW X5 4.8 2008 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E70 X5.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-34216764655',N'BMW X5 4.8 2008 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E70 X5.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-11141437929',N'BMW X5 4.8 2008 Valley Pan Gasket',N'11141437929',2,1,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N62 valley pan gasket.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-11141437929');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0003-OEM-37116757501',N'BMW X5 4.8 2008 Front Air Spring',N'37116757501',2,4,0,0,N'USD',0,1,3,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW E70 X5 front air spring strut.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0003-OEM-37116757501');

-- ============================================================
-- VEHICLE 4: Mercedes-Benz C230 2009 — M272 V6 2.5L
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A2711800209',N'Mercedes C230 2009 Engine Oil Filter',N'A2711800209',2,7,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 oil filter housing.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A2711800209');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A2720940004',N'Mercedes C230 2009 Engine Air Filter',N'A2720940004',2,7,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A2720940004');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A2038350047',N'Mercedes C230 2009 Cabin Air Filter',N'A2038350047',2,7,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A2038350047');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-0001592803',N'Mercedes C230 2009 Spark Plugs',N'0001592803',2,1,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-0001592803');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-2712000115',N'Mercedes C230 2009 Engine Thermostat',N'2712000115',2,20,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-2712000115');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-1122002701',N'Mercedes C230 2009 Water Pump',N'1122002701',2,20,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-1122002701');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A0024772601',N'Mercedes C230 2009 Fuel Filter',N'A0024772601',2,24,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 fuel filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A0024772601');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A0034201220',N'Mercedes C230 2009 Front Brake Pad Set',N'A0034201220',2,3,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A0034201220');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A0004215410',N'Mercedes C230 2009 Rear Brake Pad Set',N'A0004215410',2,3,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A0004215410');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0004-OEM-A0031531228',N'Mercedes C230 2009 Serpentine Belt',N'A0031531228',2,1,0,0,N'USD',0,1,4,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 serpentine belt.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0004-OEM-A0031531228');

-- ============================================================
-- VEHICLE 5: Honda Civic DX 2004 — D17A 1.7L
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-15400PLMA01',N'Honda Civic DX 2004 Engine Oil Filter',N'15400-PLM-A01',2,7,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-15400PLMA01');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-17220PLMY00',N'Honda Civic DX 2004 Engine Air Filter',N'17220-PLM-Y00',2,7,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-17220PLMY00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-80292SDA407',N'Honda Civic DX 2004 Cabin Air Filter',N'80292-SDA-407',2,7,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-80292SDA407');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-12290PLMA01',N'Honda Civic DX 2004 Spark Plug',N'12290-PLM-A01',2,1,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-12290PLMA01');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-19301PLMA00',N'Honda Civic DX 2004 Engine Thermostat',N'19301-PLM-A00',2,20,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-19301PLMA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-19200PLMA01',N'Honda Civic DX 2004 Water Pump',N'19200-PLM-A01',2,20,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-19200PLMA01');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-45022S5AA00',N'Honda Civic DX 2004 Front Brake Pad Set',N'45022-S5A-A00',2,3,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda front brake pads 2004 Civic.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-45022S5AA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-06450S5AA00',N'Honda Civic DX 2004 Rear Brake Pad Set',N'06450-S5A-A00',2,3,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda rear brake pads 2004 Civic.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-06450S5AA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0005-OEM-31200PLMA02',N'Honda Civic DX 2004 Starter Motor',N'31200-PLM-A02',2,5,0,0,N'USD',0,1,5,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda D17 starter motor.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0005-OEM-31200PLMA02');

-- ============================================================
-- VEHICLE 6: Honda Civic LX 2008 — R18A 1.8L
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-15400RTA003',N'Honda Civic LX 2008 Engine Oil Filter',N'15400-RTA-003',2,7,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda R18 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-15400RTA003');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-17220RNAA00',N'Honda Civic LX 2008 Engine Air Filter',N'17220-RNA-A00',2,7,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda R18 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-17220RNAA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-80292SDA407',N'Honda Civic LX 2008 Cabin Air Filter',N'80292-SDA-407',2,7,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-80292SDA407');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-12290RNAA01',N'Honda Civic LX 2008 Spark Plug',N'12290-RNA-A01',2,1,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda R18 spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-12290RNAA01');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-19301RNAA01',N'Honda Civic LX 2008 Engine Thermostat',N'19301-RNA-A01',2,20,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda R18 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-19301RNAA01');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-45022SNEA00',N'Honda Civic LX 2008 Front Brake Pad Set',N'45022-SNE-A00',2,3,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda front brake pads 2008 Civic.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-45022SNEA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-43022SNEA00',N'Honda Civic LX 2008 Rear Brake Pad Set',N'43022-SNE-A00',2,3,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda rear brake pads 2008 Civic.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-43022SNEA00');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0006-OEM-30520RNAA01',N'Honda Civic LX 2008 Ignition Coil',N'30520-RNA-A01',2,5,0,0,N'USD',0,1,6,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Honda R18 ignition coil (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0006-OEM-30520RNAA01');

-- ============================================================
-- VEHICLE 7: BMW 535i 2010 — N54 Twin-Turbo (E60)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-11427953125',N'BMW 535i 2010 Engine Oil Filter Kit',N'11427953125',2,7,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-11427953125');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-13717536006',N'BMW 535i 2010 Engine Air Filter',N'13717536006',2,7,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-13717536006');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-64316946674',N'BMW 535i 2010 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-12122158253',N'BMW 535i 2010 Iridium Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 iridium spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-11537585907',N'BMW 535i 2010 Engine Thermostat',N'11537585907',2,20,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-11537585907');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-11517583787',N'BMW 535i 2010 Electric Water Pump',N'11517583787',2,20,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 electric water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-11517583787');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-13537585261',N'BMW 535i 2010 High-Pressure Fuel Injector',N'13537585261',2,24,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 HPFP fuel injector.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-13537585261');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-11367585425',N'BMW 535i 2010 VANOS Intake Solenoid',N'11367585425',2,5,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-11367585425');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-11317512081',N'BMW 535i 2010 Timing Chain Tensioner',N'11317512081',2,1,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 timing chain tensioner.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-11317512081');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-34116776019',N'BMW 535i 2010 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E60.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-34216764655',N'BMW 535i 2010 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E60.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0007-OEM-12137592232',N'BMW 535i 2010 Ignition Coil',N'12137592232',2,5,0,0,N'USD',0,1,7,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 ignition coil (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0007-OEM-12137592232');

-- ============================================================
-- VEHICLE 8: BMW 750i 2012 — N63 V8 Twin-Turbo (F01)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-11427953125',N'BMW 750i 2012 Engine Oil Filter Kit',N'11427953125',2,7,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-11427953125');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-13717584455',N'BMW 750i 2012 Engine Air Filter',N'13717584455',2,7,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 V8 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-13717584455');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-64316946674',N'BMW 750i 2012 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-12122158253',N'BMW 750i 2012 Spark Plug N63 V8',N'12122158253',2,1,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 spark plug (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-11537573752',N'BMW 750i 2012 Engine Thermostat',N'11537573752',2,20,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-11537573752');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-11517632426',N'BMW 750i 2012 Electric Water Pump',N'11517632426',2,20,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 electric water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-11517632426');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-34116776019',N'BMW 750i 2012 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads F01.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-34216764655',N'BMW 750i 2012 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads F01.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-37206789450',N'BMW 750i 2012 Rear Air Spring',N'37206789450',2,4,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW F01 rear air spring.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-37206789450');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0008-OEM-12137592232',N'BMW 750i 2012 Ignition Coil N63',N'12137592232',2,5,0,0,N'USD',0,1,8,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N63 ignition coil (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0008-OEM-12137592232');

-- ============================================================
-- VEHICLE 9: Audi Q7 2009 — BHK 3.6L V6 FSI
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-079115561D',N'Audi Q7 2009 Engine Oil Filter',N'079115561D',2,7,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi BHK V6 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-079115561D');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-4L0133843B',N'Audi Q7 2009 Engine Air Filter',N'4L0133843B',2,7,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-4L0133843B');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-7L0819631',N'Audi Q7 2009 Cabin Air Filter',N'7L0819631',2,7,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-7L0819631');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-101905631E',N'Audi Q7 2009 Spark Plug V6',N'101905631E',2,1,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi BHK V6 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-101905631E');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-4F0819439A',N'Audi Q7 2009 Cabin Air Filter Set',N'4F0819439A',2,7,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 secondary cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-4F0819439A');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-4L0698151F',N'Audi Q7 2009 Front Brake Pad Set',N'4L0698151F',2,3,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-4L0698151F');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-4L0698451D',N'Audi Q7 2009 Rear Brake Pad Set',N'4L0698451D',2,3,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-4L0698451D');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0009-OEM-4L0616040AA',N'Audi Q7 2009 Rear Air Spring',N'4L0616040AA',2,4,0,0,N'USD',0,1,9,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi Q7 rear air suspension spring.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0009-OEM-4L0616040AA');

-- ============================================================
-- VEHICLE 10: Nissan Armada 2008 — VK56DE 5.6L V8
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-152089FE0A',N'Nissan Armada 2008 Engine Oil Filter',N'15208-9FE0A',2,7,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VK56 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-152089FE0A');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-165467S000',N'Nissan Armada 2008 Engine Air Filter',N'16546-7S000',2,7,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Armada air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-165467S000');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-27277ZM000',N'Nissan Armada 2008 Cabin Air Filter',N'27277-ZM000',2,7,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-27277ZM000');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-224015M015',N'Nissan Armada 2008 Spark Plug',N'22401-5M015',2,1,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VK56 spark plug (8 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-224015M015');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-2149500Q0C',N'Nissan Armada 2008 Engine Thermostat',N'21495-00Q0C',2,20,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VK56 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-2149500Q0C');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-210105Y700',N'Nissan Armada 2008 Water Pump',N'21010-5Y700',2,20,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VK56 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-210105Y700');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-410116Z800',N'Nissan Armada 2008 Front Brake Pad Set',N'41011-6Z800',2,3,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Armada front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-410116Z800');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-D40606Z800',N'Nissan Armada 2008 Rear Brake Pad Set',N'D4060-6Z800',2,3,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Armada rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-D40606Z800');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0010-OEM-220606Z810',N'Nissan Armada 2008 Front Ignition Coil',N'22006-6Z810',2,5,0,0,N'USD',0,1,10,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VK56 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0010-OEM-220606Z810');

-- ============================================================
-- VEHICLE 11: Nissan Pathfinder 2003 — VQ35DE 3.5L V6
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-1520865F0A',N'Nissan Pathfinder 2003 Engine Oil Filter',N'15208-65F0A',2,7,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VQ35 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-1520865F0A');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-165462Y000',N'Nissan Pathfinder 2003 Engine Air Filter',N'16546-2Y000',2,7,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Pathfinder air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-165462Y000');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-27277AU010',N'Nissan Pathfinder 2003 Cabin Air Filter',N'27277-AU010',2,7,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-27277AU010');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-22401AR215',N'Nissan Pathfinder 2003 Spark Plug',N'22401-AR215',2,1,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VQ35 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-22401AR215');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-210104W010',N'Nissan Pathfinder 2003 Water Pump',N'21010-4W010',2,20,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VQ35 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-210104W010');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-410118J005',N'Nissan Pathfinder 2003 Front Brake Pad Set',N'41011-8J005',2,3,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Pathfinder front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-410118J005');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-D40608J000',N'Nissan Pathfinder 2003 Rear Brake Pad Set',N'D4060-8J000',2,3,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan Pathfinder rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-D40608J000');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0011-OEM-224018J115',N'Nissan Pathfinder 2003 Ignition Coil',N'22448-8J115',2,5,0,0,N'USD',0,1,11,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Nissan VQ35 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0011-OEM-224488J115');

-- ============================================================
-- VEHICLE 12: GMC Terrain 2009 — LAF Ecotec 2.4L
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-12607752',N'GMC Terrain 2009 Engine Oil Filter',N'12607752',2,7,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM LAF Ecotec oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-12607752');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-25177965',N'GMC Terrain 2009 Engine Air Filter',N'25177965',2,7,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM Terrain air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-25177965');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-84152114',N'GMC Terrain 2009 Cabin Air Filter',N'84152114',2,7,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-84152114');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-12622441',N'GMC Terrain 2009 Spark Plug',N'12622441',2,1,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM LAF Ecotec spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-12622441');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-55593090',N'GMC Terrain 2009 Engine Thermostat',N'55593090',2,20,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM Ecotec 2.4 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-55593090');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-25192228',N'GMC Terrain 2009 Water Pump',N'25192228',2,20,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM Ecotec 2.4 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-25192228');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-84184999',N'GMC Terrain 2009 Front Brake Pad Set',N'84184999',2,3,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM Terrain front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-84184999');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0012-OEM-19213554',N'GMC Terrain 2009 Ignition Coil',N'19213554',2,5,0,0,N'USD',0,1,12,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine GM Ecotec ignition coil (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0012-OEM-19213554');

-- ============================================================
-- VEHICLE 13: Mitsubishi Lancer ES 2015 — 4B11 2.0L
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-MD360935',N'Mitsubishi Lancer 2015 Engine Oil Filter',N'MD360935',2,7,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi 4B11 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-MD360935');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-MR968274',N'Mitsubishi Lancer 2015 Engine Air Filter',N'MR968274',2,7,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi Lancer air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-MR968274');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-7803A004',N'Mitsubishi Lancer 2015 Cabin Air Filter',N'7803A004',2,7,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi cabin air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-7803A004');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-1822A030',N'Mitsubishi Lancer 2015 Spark Plug',N'1822A030',2,1,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi 4B11 iridium spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-1822A030');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-1305A119',N'Mitsubishi Lancer 2015 Engine Thermostat',N'1305A119',2,20,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi 4B11 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-1305A119');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-1300A062',N'Mitsubishi Lancer 2015 Water Pump',N'1300A062',2,20,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi 4B11 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-1300A062');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-4605A486',N'Mitsubishi Lancer 2015 Front Brake Pad Set',N'4605A486',2,3,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi Lancer front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-4605A486');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-4605A514',N'Mitsubishi Lancer 2015 Rear Brake Pad Set',N'4605A514',2,3,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi Lancer rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-4605A514');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0013-OEM-1832A031',N'Mitsubishi Lancer 2015 Ignition Coil',N'1832A031',2,5,0,0,N'USD',0,1,13,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mitsubishi 4B11 ignition coil (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0013-OEM-1832A031');

-- ============================================================
-- VEHICLE 14: Mercedes-Benz C300 2009 — M272 V6 3.0L
-- (reuses same M272 parts as C230, different vehicle ID)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A2711800209',N'Mercedes C300 2009 Engine Oil Filter',N'A2711800209',2,7,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A2711800209');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A2720940004',N'Mercedes C300 2009 Engine Air Filter',N'A2720940004',2,7,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A2720940004');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A2038350047',N'Mercedes C300 2009 Cabin Air Filter',N'A2038350047',2,7,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A2038350047');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-0001592803',N'Mercedes C300 2009 Spark Plugs',N'0001592803',2,1,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-0001592803');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-2712000115',N'Mercedes C300 2009 Engine Thermostat',N'2712000115',2,20,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-2712000115');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-1122002701',N'Mercedes C300 2009 Water Pump',N'1122002701',2,20,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-1122002701');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A0034201220',N'Mercedes C300 2009 Front Brake Pad Set',N'A0034201220',2,3,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A0034201220');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A0004215410',N'Mercedes C300 2009 Rear Brake Pad Set',N'A0004215410',2,3,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A0004215410');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0014-OEM-A0024772601',N'Mercedes C300 2009 Fuel Filter',N'A0024772601',2,24,0,0,N'USD',0,1,14,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 fuel filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0014-OEM-A0024772601');

-- ============================================================
-- VEHICLE 15: Mercedes-Benz E350 2008 — M272 V6 3.5L (W211)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-A2711800209',N'Mercedes E350 2008 Engine Oil Filter',N'A2711800209',2,7,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-A2711800209');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-A2720940004',N'Mercedes E350 2008 Engine Air Filter',N'A2720940004',2,7,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-A2720940004');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-A2118300318',N'Mercedes E350 2008 Cabin Air Filter',N'A2118300318',2,7,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes W211 E-Class cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-A2118300318');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-0001592803',N'Mercedes E350 2008 Spark Plug',N'0001592803',2,1,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-0001592803');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-2712000115',N'Mercedes E350 2008 Engine Thermostat',N'2712000115',2,20,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-2712000115');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-1122002701',N'Mercedes E350 2008 Water Pump',N'1122002701',2,20,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-1122002701');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-A0034202720',N'Mercedes E350 2008 Front Brake Pad Set',N'A0034202720',2,3,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes E-Class front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-A0034202720');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0015-OEM-A0024772601',N'Mercedes E350 2008 Fuel Filter',N'A0024772601',2,24,0,0,N'USD',0,1,15,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 fuel filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0015-OEM-A0024772601');

-- ============================================================
-- VEHICLE 16: Mercedes-Benz E350 Coupe 2012 — M272 V6 (C207)
-- ============================================================
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-A2711800209',N'Mercedes E350 Coupe 2012 Engine Oil Filter',N'A2711800209',2,7,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-A2711800209');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-A2720940004',N'Mercedes E350 Coupe 2012 Engine Air Filter',N'A2720940004',2,7,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-A2720940004');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-A2118300318',N'Mercedes E350 Coupe 2012 Cabin Air Filter',N'A2118300318',2,7,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes E-Class cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-A2118300318');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-0001592803',N'Mercedes E350 Coupe 2012 Spark Plug',N'0001592803',2,1,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-0001592803');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-2712000115',N'Mercedes E350 Coupe 2012 Engine Thermostat',N'2712000115',2,20,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-2712000115');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-A0034202720',N'Mercedes E350 Coupe 2012 Front Brake Pad Set',N'A0034202720',2,3,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes front brake pads C207.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-A0034202720');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0016-OEM-1122002701',N'Mercedes E350 Coupe 2012 Water Pump',N'1122002701',2,20,0,0,N'USD',0,1,16,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0016-OEM-1122002701');

-- ============================================================
-- VEHICLES 17-19,21,25,26,28,31: BMW N52 Inline-6 Group
-- V17=525i 2006, V18=535i 2008(N54), V19=325i 2006,
-- V21=328i 2008, V25=X3 3.0 2010, V26=328i 2011,
-- V28=X3 3.0 2008, V31=328i 2007
-- NOTE: V18 535i 2008 uses N54, same parts as V1/V7
-- ============================================================

-- VEHICLE 17: BMW 525i 2006 — N52 (E60)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-11427512300',N'BMW 525i 2006 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-11427512300');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-13717548994',N'BMW 525i 2006 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-13717548994');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-64316946674',N'BMW 525i 2006 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-12122158253',N'BMW 525i 2006 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 iridium spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-11530420830',N'BMW 525i 2006 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-11530420830');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-11517586925',N'BMW 525i 2006 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-11517586925');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-11367545862',N'BMW 525i 2006 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS intake solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-11367545862');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-34116776019',N'BMW 525i 2006 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E60.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-34216764655',N'BMW 525i 2006 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E60.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0017-OEM-17137534065',N'BMW 525i 2006 Coolant Expansion Tank',N'17137534065',2,20,0,0,N'USD',0,1,17,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW coolant expansion tank.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0017-OEM-17137534065');

-- VEHICLE 18: BMW 535i 2008 — N54 Twin-Turbo (E60)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-11427953125',N'BMW 535i 2008 Engine Oil Filter Kit',N'11427953125',2,7,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-11427953125');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-13717536006',N'BMW 535i 2008 Engine Air Filter',N'13717536006',2,7,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-13717536006');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-64316946674',N'BMW 535i 2008 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-12122158253',N'BMW 535i 2008 Iridium Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 iridium spark plug.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-11537585907',N'BMW 535i 2008 Engine Thermostat',N'11537585907',2,20,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-11537585907');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-11517583787',N'BMW 535i 2008 Electric Water Pump',N'11517583787',2,20,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 electric water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-11517583787');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-11367585425',N'BMW 535i 2008 VANOS Intake Solenoid',N'11367585425',2,5,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-11367585425');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-34116776019',N'BMW 535i 2008 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E60.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0018-OEM-12137592232',N'BMW 535i 2008 Ignition Coil',N'12137592232',2,5,0,0,N'USD',0,1,18,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N54 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0018-OEM-12137592232');

-- VEHICLE 19: BMW 325i 2006 — N52 (E90)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-11427512300',N'BMW 325i 2006 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-11427512300');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-13717548994',N'BMW 325i 2006 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-13717548994');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-64316946674',N'BMW 325i 2006 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-12122158253',N'BMW 325i 2006 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 iridium spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-11530420830',N'BMW 325i 2006 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-11530420830');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-11517586925',N'BMW 325i 2006 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-11517586925');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-11367545862',N'BMW 325i 2006 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-11367545862');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-34116776019',N'BMW 325i 2006 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0019-OEM-34216764655',N'BMW 325i 2006 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,19,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0019-OEM-34216764655');

-- VEHICLE 20: Audi A4 2.0T 2009 — CAEB EA888
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-06D115562',N'Audi A4 2.0T 2009 Engine Oil Filter',N'06D115562',2,7,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 oil filter housing.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-06D115562');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-8K0133843',N'Audi A4 2.0T 2009 Engine Air Filter',N'8K0133843',2,7,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8 A4 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-8K0133843');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-8K0819439A',N'Audi A4 2.0T 2009 Cabin Air Filter',N'8K0819439A',2,7,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8 A4 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-8K0819439A');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-101905611B',N'Audi A4 2.0T 2009 Spark Plug',N'101905611B',2,1,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-101905611B');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-06H121026DD',N'Audi A4 2.0T 2009 Water Pump',N'06H121026DD',2,20,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-06H121026DD');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-06H121113A',N'Audi A4 2.0T 2009 Engine Thermostat',N'06H121113A',2,20,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-06H121113A');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-8K0698151F',N'Audi A4 2.0T 2009 Front Brake Pad Set',N'8K0698151F',2,3,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8 A4 front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-8K0698151F');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-8K0698451G',N'Audi A4 2.0T 2009 Rear Brake Pad Set',N'8K0698451G',2,3,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8 A4 rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-8K0698451G');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0020-OEM-06H905115E',N'Audi A4 2.0T 2009 Ignition Coil',N'06H905115E',2,5,0,0,N'USD',0,1,20,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0020-OEM-06H905115E');

-- VEHICLE 21: BMW 328i 2008 — N52 (E90/E92)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-11427512300',N'BMW 328i 2008 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-11427512300');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-13717548994',N'BMW 328i 2008 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-13717548994');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-64316946674',N'BMW 328i 2008 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-64316946674');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-12122158253',N'BMW 328i 2008 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 iridium spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-12122158253');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-11530420830',N'BMW 328i 2008 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-11530420830');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-11517586925',N'BMW 328i 2008 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-11517586925');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-34116776019',N'BMW 328i 2008 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-34116776019');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-34216764655',N'BMW 328i 2008 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-34216764655');

INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0021-OEM-11367545862',N'BMW 328i 2008 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,21,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0021-OEM-11367545862');

-- VEHICLE 22: Audi A4 2.0T 2012 — CDNB EA888 Gen2 (B8.5)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-06D115562',N'Audi A4 2.0T 2012 Engine Oil Filter',N'06D115562',2,7,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 Gen2 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-06D115562');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-8K0133843',N'Audi A4 2.0T 2012 Engine Air Filter',N'8K0133843',2,7,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 A4 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-8K0133843');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-8K0819439A',N'Audi A4 2.0T 2012 Cabin Air Filter',N'8K0819439A',2,7,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 A4 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-8K0819439A');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-101905611B',N'Audi A4 2.0T 2012 Spark Plug',N'101905611B',2,1,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 Gen2 spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-101905611B');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-06H121026DD',N'Audi A4 2.0T 2012 Water Pump',N'06H121026DD',2,20,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-06H121026DD');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-06H121113A',N'Audi A4 2.0T 2012 Engine Thermostat',N'06H121113A',2,20,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-06H121113A');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-8K0698151F',N'Audi A4 2.0T 2012 Front Brake Pad Set',N'8K0698151F',2,3,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-8K0698151F');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0022-OEM-8K0698451G',N'Audi A4 2.0T 2012 Rear Brake Pad Set',N'8K0698451G',2,3,0,0,N'USD',0,1,22,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0022-OEM-8K0698451G');

-- VEHICLE 23: Audi A4 2.0T 2007 — BWT (B7 platform)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-06D115562',N'Audi A4 2.0T 2007 Engine Oil Filter',N'06D115562',2,7,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi BWT oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-06D115562');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-8E0133843',N'Audi A4 2.0T 2007 Engine Air Filter',N'8E0133843',2,7,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B7 A4 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-8E0133843');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-8E0819439',N'Audi A4 2.0T 2007 Cabin Air Filter',N'8E0819439',2,7,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B7 A4 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-8E0819439');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-101905611B',N'Audi A4 2.0T 2007 Spark Plug',N'101905611B',2,1,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi BWT spark plug (4 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-101905611B');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-8E0698151L',N'Audi A4 2.0T 2007 Front Brake Pad Set',N'8E0698151L',2,3,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B7 A4 front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-8E0698151L');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-8E0698451D',N'Audi A4 2.0T 2007 Rear Brake Pad Set',N'8E0698451D',2,3,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B7 A4 rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-8E0698451D');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0023-OEM-06H121026DD',N'Audi A4 2.0T 2007 Water Pump',N'06H121026DD',2,20,0,0,N'USD',0,1,23,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi BWT water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0023-OEM-06H121026DD');

-- VEHICLE 24: Audi A4 2.0T 2012 second unit — CDNB
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-06D115562',N'Audi A4 2.0T 2012 #2 Engine Oil Filter',N'06D115562',2,7,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 Gen2 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-06D115562');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-8K0133843',N'Audi A4 2.0T 2012 #2 Engine Air Filter',N'8K0133843',2,7,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 A4 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-8K0133843');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-8K0819439A',N'Audi A4 2.0T 2012 #2 Cabin Air Filter',N'8K0819439A',2,7,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 A4 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-8K0819439A');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-101905611B',N'Audi A4 2.0T 2012 #2 Spark Plug',N'101905611B',2,1,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 Gen2 spark plug.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-101905611B');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-06H121026DD',N'Audi A4 2.0T 2012 #2 Water Pump',N'06H121026DD',2,20,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi EA888 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-06H121026DD');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-8K0698151F',N'Audi A4 2.0T 2012 #2 Front Brake Pad Set',N'8K0698151F',2,3,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-8K0698151F');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0024-OEM-8K0698451G',N'Audi A4 2.0T 2012 #2 Rear Brake Pad Set',N'8K0698451G',2,3,0,0,N'USD',0,1,24,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Audi B8.5 rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0024-OEM-8K0698451G');

-- VEHICLE 25: BMW X3 3.0 2010 — N52 (E83)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-11427512300',N'BMW X3 3.0 2010 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-13717548994',N'BMW X3 3.0 2010 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-13717548994');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-64316946674',N'BMW X3 3.0 2010 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-12122158253',N'BMW X3 3.0 2010 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-11530420830',N'BMW X3 3.0 2010 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-11530420830');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-11517586925',N'BMW X3 3.0 2010 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-11517586925');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-34116776019',N'BMW X3 3.0 2010 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-34216764655',N'BMW X3 3.0 2010 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0025-OEM-11367545862',N'BMW X3 3.0 2010 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,25,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0025-OEM-11367545862');

-- VEHICLE 26: BMW 328i 2011 — N52 (E90 LCI)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-11427512300',N'BMW 328i 2011 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-13717548994',N'BMW 328i 2011 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-13717548994');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-64316946674',N'BMW 328i 2011 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-12122158253',N'BMW 328i 2011 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-11530420830',N'BMW 328i 2011 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-11530420830');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-11517586925',N'BMW 328i 2011 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-11517586925');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-34116776019',N'BMW 328i 2011 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-34216764655',N'BMW 328i 2011 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0026-OEM-11367545862',N'BMW 328i 2011 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,26,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0026-OEM-11367545862');

-- VEHICLE 27: BMW X3 2.5 2006 — M54 (E83)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-11427512300',N'BMW X3 2.5 2006 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW M54 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-13717501579',N'BMW X3 2.5 2006 Engine Air Filter',N'13717501579',2,7,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW M54 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-13717501579');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-64316946674',N'BMW X3 2.5 2006 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-12120039804',N'BMW X3 2.5 2006 Spark Plug M54',N'12120039804',2,1,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW M54 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-12120039804');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-11531707010',N'BMW X3 2.5 2006 Engine Thermostat',N'11531707010',2,20,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW M54 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-11531707010');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-11517527799',N'BMW X3 2.5 2006 Water Pump',N'11517527799',2,20,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW M54 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-11517527799');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-34116776019',N'BMW X3 2.5 2006 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0027-OEM-34216764655',N'BMW X3 2.5 2006 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,27,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0027-OEM-34216764655');

-- VEHICLE 28: BMW X3 3.0 2008 — N52 (E83 facelift)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-11427512300',N'BMW X3 3.0 2008 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-13717548994',N'BMW X3 3.0 2008 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-13717548994');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-64316946674',N'BMW X3 3.0 2008 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-12122158253',N'BMW X3 3.0 2008 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-11530420830',N'BMW X3 3.0 2008 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-11530420830');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-11517586925',N'BMW X3 3.0 2008 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-11517586925');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-34116776019',N'BMW X3 3.0 2008 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-34216764655',N'BMW X3 3.0 2008 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E83.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0028-OEM-11367545862',N'BMW X3 3.0 2008 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,28,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0028-OEM-11367545862');

-- VEHICLE 29: BMW 535i 2013 — N55 (F10)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-11428507683',N'BMW 535i 2013 Engine Oil Filter Kit',N'11428507683',2,7,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-11428507683');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-13717576462',N'BMW 535i 2013 Engine Air Filter',N'13717576462',2,7,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-13717576462');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-64316946674',N'BMW 535i 2013 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-12122158253',N'BMW 535i 2013 Spark Plug N55',N'12122158253',2,1,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-11537549476',N'BMW 535i 2013 Engine Thermostat',N'11537549476',2,20,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-11537549476');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-11517597715',N'BMW 535i 2013 Electric Water Pump',N'11517597715',2,20,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-11517597715');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-34116776019',N'BMW 535i 2013 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads F10.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-34216764655',N'BMW 535i 2013 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads F10.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0029-OEM-12137590261',N'BMW 535i 2013 Ignition Coil N55',N'12137590261',2,5,0,0,N'USD',0,1,29,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0029-OEM-12137590261');

-- VEHICLE 30: Mercedes-Benz C280 2007 — M272 (W203)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A2711800209',N'Mercedes C280 2007 Engine Oil Filter',N'A2711800209',2,7,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 oil filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A2711800209');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A2720940004',N'Mercedes C280 2007 Engine Air Filter',N'A2720940004',2,7,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A2720940004');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A2038350047',N'Mercedes C280 2007 Cabin Air Filter',N'A2038350047',2,7,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes W203 cabin filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A2038350047');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-0001592803',N'Mercedes C280 2007 Spark Plug',N'0001592803',2,1,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-0001592803');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-2712000115',N'Mercedes C280 2007 Engine Thermostat',N'2712000115',2,20,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-2712000115');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-1122002701',N'Mercedes C280 2007 Water Pump',N'1122002701',2,20,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-1122002701');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A0034201220',N'Mercedes C280 2007 Front Brake Pad Set',N'A0034201220',2,3,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class front brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A0034201220');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A0004215410',N'Mercedes C280 2007 Rear Brake Pad Set',N'A0004215410',2,3,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes C-Class rear brake pads.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A0004215410');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0030-OEM-A0031531228',N'Mercedes C280 2007 Serpentine Belt',N'A0031531228',2,1,0,0,N'USD',0,1,30,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine Mercedes M272 serpentine belt.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0030-OEM-A0031531228');

-- VEHICLE 31: BMW 328i 2007 — N52 (E90)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-11427512300',N'BMW 328i 2007 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-13717548994',N'BMW 328i 2007 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-13717548994');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-64316946674',N'BMW 328i 2007 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-12122158253',N'BMW 328i 2007 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-11530420830',N'BMW 328i 2007 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-11530420830');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-11517586925',N'BMW 328i 2007 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-11517586925');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-34116776019',N'BMW 328i 2007 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-34216764655',N'BMW 328i 2007 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E90.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0031-OEM-11367545862',N'BMW 328i 2007 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,31,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0031-OEM-11367545862');

-- VEHICLE 32: BMW X5 3.0 2007 — N52 (E70)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-11427512300',N'BMW X5 3.0 2007 Engine Oil Filter Kit',N'11427512300',2,7,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-11427512300');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-13717548994',N'BMW X5 3.0 2007 Engine Air Filter',N'13717548994',2,7,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-13717548994');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-64316946674',N'BMW X5 3.0 2007 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-12122158253',N'BMW X5 3.0 2007 Spark Plug',N'12122158253',2,1,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-11530420830',N'BMW X5 3.0 2007 Engine Thermostat',N'11530420830',2,20,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-11530420830');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-11517586925',N'BMW X5 3.0 2007 Water Pump',N'11517586925',2,20,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-11517586925');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-34116776019',N'BMW X5 3.0 2007 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads E70.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-34216764655',N'BMW X5 3.0 2007 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads E70.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-11367545862',N'BMW X5 3.0 2007 VANOS Intake Solenoid',N'11367545862',2,5,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N52 VANOS solenoid.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-11367545862');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0032-OEM-37116757502',N'BMW X5 3.0 2007 Front Air Spring',N'37116757502',2,4,0,0,N'USD',0,1,32,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW E70 front air spring.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0032-OEM-37116757502');

-- VEHICLE 33: BMW X6 3.5 2013 — N55 (F16)
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-11428507683',N'BMW X6 3.5 2013 Engine Oil Filter Kit',N'11428507683',2,7,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 oil filter kit.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-11428507683');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-13717576462',N'BMW X6 3.5 2013 Engine Air Filter',N'13717576462',2,7,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 air filter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-13717576462');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-64316946674',N'BMW X6 3.5 2013 Cabin Air Filter',N'64316946674',2,7,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW cabin microfilter.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-64316946674');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-12122158253',N'BMW X6 3.5 2013 Spark Plug N55',N'12122158253',2,1,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 spark plug (6 required).',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-12122158253');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-11537549476',N'BMW X6 3.5 2013 Engine Thermostat',N'11537549476',2,20,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 thermostat.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-11537549476');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-11517597715',N'BMW X6 3.5 2013 Electric Water Pump',N'11517597715',2,20,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 water pump.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-11517597715');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-34116776019',N'BMW X6 3.5 2013 Front Brake Pad Set',N'34116776019',2,3,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW front brake pads F16.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-34116776019');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-34216764655',N'BMW X6 3.5 2013 Rear Brake Pad Set',N'34216764655',2,3,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW rear brake pads F16.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-34216764655');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-12137590261',N'BMW X6 3.5 2013 Ignition Coil N55',N'12137590261',2,5,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW N55 ignition coil.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-12137590261');
INSERT INTO dbo.Parts (InternalCode,Name,OEMNumber,Condition,CategoryId,CostPrice,SalePrice,Currency,MinStock,IsActive,UsedCarID,AllocatedCost,MinimumSellPrice,FastSalePrice,WholesalePrice,RecommendedPrice,PricingStatus,CostAllocationPercent,ImageUrls,Notes,CreatedAt)
SELECT 'UC0033-OEM-37116757503',N'BMW X6 3.5 2013 Rear Air Spring',N'37116757503',2,4,0,0,N'USD',0,1,33,0,0,0,0,0,N'Manual',0,N'[]',N'Genuine BMW F16 rear air spring.',SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parts WHERE InternalCode='UC0033-OEM-37116757503');

-- ============================================================
-- VERIFICATION QUERY (uncomment to run after INSERT batch)
-- SELECT UsedCarID, COUNT(*) AS PartsInserted
-- FROM dbo.Parts WHERE InternalCode LIKE 'UC%OEM%'
-- GROUP BY UsedCarID ORDER BY UsedCarID;
-- ============================================================
