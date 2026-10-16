@echo off
rem ============================================================
rem  UYARI (Paket T-1, MUHUR 3 "tek yayin yolu"):
rem  BU DOSYA YALNIZ GELISTIRME ICINDIR - URETIM YAYIN YOLU DEGILDIR.
rem  .NET SDK + "dotnet run" + ASPNETCORE_ENVIRONMENT=Development dayatir;
rem  ucu de uretimde YASAKTIR (FAIL-CLOSED 6).
rem  Uretim kurulumu: deploy\kuyumcu-service-kur.ps1 (Windows servisi),
rem  deploy\systemd\kuyumcu.service (Linux) ya da docker-compose.uretim.yml.
rem  Rehber: docs\DEPLOY.md
rem  Dosyanin silinmesi kurucunun kararina birakildi.
rem ============================================================
rem ============================================================
rem  MIHENK (.NET / Blazor) - Baslatma
rem  Uygulama: http://localhost:5000
rem  (ASP.NET Core API + Blazor WASM tek surecte sunulur)
rem ============================================================
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo  ============================================
echo   MIHENK baslatiliyor...
echo  ============================================
echo.

rem --- .NET SDK kontrolu ---
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [HATA] .NET SDK bulunamadi. Kurulum: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

rem --- MySQL (Docker) ---
where docker >nul 2>nul
if errorlevel 1 (
    echo [UYARI] Docker bulunamadi. MySQL'in elle calistigindan emin olun (localhost:3306).
) else (
    echo [BILGI] MySQL konteyneri baslatiliyor...
    docker compose up -d mysql
    echo [BILGI] MySQL'in hazir olmasi bekleniyor...
    set /a DENEME=0
    :mysql_bekle
    docker inspect --format "{{.State.Health.Status}}" kuyumcu-mysql 2>nul | findstr "healthy" >nul 2>nul
    if errorlevel 1 (
        set /a DENEME+=1
        if !DENEME! geq 30 (
            echo [HATA] MySQL 90 saniyede hazir olmadi. Loglar: docker logs kuyumcu-mysql
            pause
            exit /b 1
        )
        timeout /t 3 /nobreak >nul
        goto mysql_bekle
    )
    echo [BILGI] MySQL hazir.
)

rem --- port 5000 kontrolu ---
netstat -ano | findstr "LISTENING" | findstr ":5000 " >nul 2>nul
if not errorlevel 1 (
    echo [UYARI] 5000 portu zaten kullanimda. Once stop.bat calistirin.
    pause
    exit /b 1
)

rem --- API + Blazor (port 5000) ---
echo [BILGI] Uygulama baslatiliyor  -^> http://localhost:5000
start "kuyumcu-net" cmd /k "cd /d "%~dp0dotnet\Kuyumcu.Api" && set ASPNETCORE_ENVIRONMENT=Development&& set ASPNETCORE_URLS=http://localhost:5000&& dotnet run -c Release"

rem --- tarayiciyi ac (ilk derleme biraz surebilir) ---
echo [BILGI] 20 saniye sonra tarayici acilacak...
timeout /t 20 /nobreak >nul
start http://localhost:5000

echo.
echo  ============================================
echo   MIHENK calisiyor!  http://localhost:5000
echo   Kapatmak icin stop.bat dosyasini calistirin.
echo  ============================================
echo.
endlocal
exit /b 0
