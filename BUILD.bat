@echo off
title Build - IT Help Desk Lite
setlocal
cd /d "%~dp0"
set "VER=1.1.0"
set "APP=IT Help Desk Lite"

rem ---- C# compiler that is already inside Windows (.NET Framework 4.x) ----
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework 4 compiler not found. Enable .NET Framework 4.8 in Windows Features.
  pause
  exit /b 1
)

if not exist dist mkdir dist
set "REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Management.dll"

echo [1/3] Building %APP%.exe ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ %REFS% ^
  /win32manifest:src\app.manifest /win32icon:src\app.ico ^
  /out:"dist\%APP%.exe" src\Program.cs
if errorlevel 1 goto fail
copy /y ithelp.ini "dist\ithelp.ini" >nul 2>&1

echo [2/3] Making portable ZIP ...
if exist "dist\ITHelpDeskLite-Portable-%VER%.zip" del "dist\ITHelpDeskLite-Portable-%VER%.zip"
powershell -NoProfile -Command "Compress-Archive -Path 'dist\%APP%.exe','dist\ithelp.ini','LICENSE','README.md' -DestinationPath 'dist\ITHelpDeskLite-Portable-%VER%.zip'"

echo [3/3] Making Setup.exe installer ...
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo.
  echo   Inno Setup 6 is not installed, so Setup.exe was skipped.
  echo   Install it once ^(free^):   winget install JRSoftware.InnoSetup
  echo   then run BUILD.bat again.
  goto done
)
"%ISCC%" /Q setup.iss
if errorlevel 1 goto fail

:done
echo.
echo ============================================================
echo  DONE.  Files in the dist folder:
echo    %APP%.exe  +  ithelp.ini        ^(run directly^)
echo    ITHelpDeskLite-Portable-%VER%.zip  ^(for GitHub Releases^)
echo    ITHelpDeskLite-Setup-%VER%.exe     ^(installer, for GitHub Releases^)
echo ============================================================
pause
exit /b 0

:fail
echo.
echo BUILD FAILED - copy the red lines to Claude.
pause
exit /b 1
