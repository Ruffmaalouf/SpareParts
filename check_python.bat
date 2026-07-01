@echo off
cd /d D:\ralph\spareparts
echo Python check: > check_output.txt
python --version >> check_output.txt 2>&1
where python >> check_output.txt 2>&1
echo. >> check_output.txt
echo pyodbc check: >> check_output.txt
python -c "import pyodbc; print('pyodbc OK')" >> check_output.txt 2>&1
echo. >> check_output.txt
echo requests check: >> check_output.txt
python -c "import requests; print('requests OK')" >> check_output.txt 2>&1
echo. >> check_output.txt
echo DB connection test: >> check_output.txt
python -c "import pyodbc; c=pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};Server=localhost;Database=SparePartsDb;Trusted_Connection=yes;TrustServerCertificate=yes;'); print('DB OK'); c.close()" >> check_output.txt 2>&1
echo Done. See check_output.txt
type check_output.txt
pause
