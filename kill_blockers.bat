@echo off
title Kill SparePartsDb Blockers
echo Killing all SparePartsDb sessions except SPID 79...
sqlcmd -S localhost -E -d master -Q "DECLARE @s NVARCHAR(MAX)=''; SELECT @s=@s+'KILL '+CAST(session_id AS VARCHAR)+'; ' FROM sys.dm_exec_sessions WHERE DB_NAME(database_id)='SparePartsDb' AND session_id<>79 AND session_id>50; PRINT 'Killing: '+@s; EXEC(@s); PRINT 'Done.';"
echo.
echo Blocker cleanup complete. SPID 79 should now proceed.
pause
