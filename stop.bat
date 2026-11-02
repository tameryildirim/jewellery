@echo off
rem ============================================================
rem  ByGatech (.NET / Blazor) - Durdurma
rem  5000 portunu dinleyen islemi ve acilan pencereyi kapatir.
rem  MySQL konteyneri calismaya devam eder
rem  (durdurmak icin: docker compose stop mysql).
rem ============================================================
setlocal enabledelayedexpansion

echo.
echo  ============================================
echo   ByGatech durduruluyor...
echo  ============================================
echo.

set "KAPATILDI=0"

rem --- Port 5000 uzerindeki islemi sonlandir ---
for /f "tokens=5" %%I in ('netstat -ano ^| findstr "LISTENING" ^| findstr ":5000 "') do (
    if not "%%I"=="0" (
        echo [BILGI] Port 5000 uzerindeki islem sonlandiriliyor (PID: %%I^)
        taskkill /PID %%I /T /F >nul 2>nul
        set "KAPATILDI=1"
    )
)

rem --- start.bat ile acilan pencereyi kapat ---
taskkill /FI "WINDOWTITLE eq kuyumcu-net*" /T /F >nul 2>nul

if "!KAPATILDI!"=="1" (
    echo [BILGI] ByGatech durduruldu.
) else (
    echo [BILGI] Calisan ByGatech islemi bulunamadi.
)

echo [BILGI] MySQL hala calisiyor. Durdurmak icin: docker compose stop mysql
echo.
pause
endlocal
exit /b 0
