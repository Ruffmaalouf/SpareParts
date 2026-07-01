@echo off
cd /d D:\ralph\spareparts
echo Python Launcher (py): > check_output2.txt
py --version >> check_output2.txt 2>&1
py -3 --version >> check_output2.txt 2>&1
echo. >> check_output2.txt
echo python3: >> check_output2.txt
python3 --version >> check_output2.txt 2>&1
echo. >> check_output2.txt
echo Looking for python.exe: >> check_output2.txt
where py >> check_output2.txt 2>&1
dir /b /s "C:\Python*\python.exe" >> check_output2.txt 2>&1
dir /b /s "C:\Users\r.maalouf\AppData\Local\Programs\Python\*\python.exe" >> check_output2.txt 2>&1
echo. >> check_output2.txt
echo PATH: >> check_output2.txt
echo %PATH% >> check_output2.txt
echo Done. >> check_output2.txt
type check_output2.txt
pause
