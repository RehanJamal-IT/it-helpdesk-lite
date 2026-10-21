@echo off
title Build - IT Help Desk Lite (Android)
setlocal EnableDelayedExpansion
cd /d "%~dp0"
set "VER=1.1.0"
set "GRADLE_VER=8.11.1"

echo ============================================================
echo   IT Help Desk Lite - Android build
echo   Makes: APK (to install on phones) + AAB (for Google Play)
echo   by Rehan Jamal
echo ============================================================
echo.

rem ---------------------------------------------------------- 1. Java (from Android Studio)
if defined JAVA_HOME if not exist "%JAVA_HOME%\bin\java.exe" set "JAVA_HOME="
if not defined JAVA_HOME (
  for %%P in ("%ProgramFiles%\Android\Android Studio\jbr" "%LocalAppData%\Programs\Android Studio\jbr" "D:\Android\Android Studio\jbr" "D:\Program Files\Android\Android Studio\jbr" "D:\Android Studio\jbr" "%ProgramFiles%\Android\Android Studio\jre") do (
    if not defined JAVA_HOME if exist "%%~P\bin\java.exe" set "JAVA_HOME=%%~P"
  )
)
if not defined JAVA_HOME (
  echo [X] Java not found. Install Android Studio, or set JAVA_HOME to its "jbr" folder.
  goto fail
)
echo [1/5] Java: %JAVA_HOME%

rem ---------------------------------------------------------- 2. Android SDK
if not defined ANDROID_HOME if defined ANDROID_SDK_ROOT set "ANDROID_HOME=%ANDROID_SDK_ROOT%"
if not defined ANDROID_HOME (
  for %%S in ("%LocalAppData%\Android\Sdk" "D:\Android\Sdk" "D:\Android\SDK" "C:\Android\Sdk") do (
    if not defined ANDROID_HOME if exist "%%~S\platform-tools" set "ANDROID_HOME=%%~S"
  )
)
if not defined ANDROID_HOME call :install_sdk
if not defined ANDROID_HOME goto fail
set "SDKF=!ANDROID_HOME:\=/!"
> local.properties echo sdk.dir=!SDKF!
echo [2/5] Android SDK: %ANDROID_HOME%

rem ---------------------------------------------------------- 3. Gradle (downloaded once into .\tools)
set "GDIR=%~dp0tools\gradle-%GRADLE_VER%"
if not exist "%GDIR%\bin\gradle.bat" (
  echo [3/5] Downloading Gradle %GRADLE_VER% - only the first time, please wait ...
  if not exist tools mkdir tools
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol='Tls12'; Invoke-WebRequest 'https://services.gradle.org/distributions/gradle-%GRADLE_VER%-bin.zip' -OutFile 'tools\gradle.zip'; Expand-Archive 'tools\gradle.zip' 'tools' -Force; Remove-Item 'tools\gradle.zip'"
)
if not exist "%GDIR%\bin\gradle.bat" (
  echo [X] Could not download Gradle. Check the internet connection and try again.
  goto fail
)
echo [3/5] Gradle %GRADLE_VER% ready

rem ---------------------------------------------------------- 4. Signing key (one time)
if exist keystore.properties goto key_ok
echo.
echo [4/5] Creating YOUR signing key for Google Play (one time only)
echo       Use letters and numbers only, at least 6 characters.
echo       WRITE IT DOWN - you need it for every future update.
echo.
set "KPASS="
set /p "KPASS=      Choose a key password: "
if "!KPASS!"=="" ( echo [X] No password entered. & goto fail )
"%JAVA_HOME%\bin\keytool.exe" -genkeypair -keystore upload-key.jks -alias ithelp -keyalg RSA -keysize 2048 -validity 10000 -storepass "!KPASS!" -keypass "!KPASS!" -dname "CN=Rehan Jamal, OU=IT, O=Rehan Jamal IT, L=Dubai, C=AE"
if errorlevel 1 ( echo [X] Could not create the key. & goto fail )
(
  echo storeFile=upload-key.jks
  echo storePassword=!KPASS!
  echo keyAlias=ithelp
  echo keyPassword=!KPASS!
) > keystore.properties
echo       Key created: upload-key.jks  ^(keep a backup copy on a USB drive^)
:key_ok
echo [4/5] Signing key ready

rem ---------------------------------------------------------- 5. Build
echo [5/5] Building ... the first build downloads tools and can take 5-10 minutes.
echo.
call "%GDIR%\bin\gradle.bat" --no-daemon clean assembleRelease bundleRelease
if errorlevel 1 goto fail

if not exist dist mkdir dist
copy /y "app\build\outputs\apk\release\app-release.apk" "dist\ITHelpDeskLite-%VER%.apk" >nul
copy /y "app\build\outputs\bundle\release\app-release.aab" "dist\ITHelpDeskLite-%VER%.aab" >nul

echo.
echo ============================================================
echo  DONE.  Files in the dist folder:
echo    ITHelpDeskLite-%VER%.apk   - install on any Android phone
echo    ITHelpDeskLite-%VER%.aab   - upload this to Google Play Console
echo ============================================================
pause
exit /b 0

:fail
echo.
echo BUILD FAILED.
echo  - If it says "licences not accepted" or "Android 16 / API 36 not found":
echo    open Android Studio, More Actions, SDK Manager, tick "Android 16 (API 36)", Apply.
echo  - Otherwise copy the red lines to Claude.
pause
exit /b 1

rem ==========================================================
rem  Android SDK auto-install (no Android Studio needed)
rem ==========================================================
:install_sdk
set "NEWSDK=%LocalAppData%\Android\Sdk"
if exist D:\ set "NEWSDK=D:\Android\Sdk"
echo.
echo [2/5] Android SDK not found - installing it now into %NEWSDK%
echo       (one time only, about 500 MB, please wait ...)
if not exist "%NEWSDK%" mkdir "%NEWSDK%"
set "CLT=%NEWSDK%\cmdline-tools\latest\bin\sdkmanager.bat"
if exist "%CLT%" goto sdk_tools_ok
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol='Tls12'; $d='%NEWSDK%'; $z=\"$d\clt.zip\"; $ok=$false; foreach($v in '13114758','11076708'){ try { Invoke-WebRequest \"https://dl.google.com/android/repository/commandlinetools-win-${v}_latest.zip\" -OutFile $z; $ok=$true; break } catch {} }; if(-not $ok){ exit 1 }; Expand-Archive $z \"$d\clt-tmp\" -Force; New-Item -ItemType Directory -Force \"$d\cmdline-tools\" | Out-Null; if(Test-Path \"$d\cmdline-tools\latest\"){ Remove-Item \"$d\cmdline-tools\latest\" -Recurse -Force }; Move-Item \"$d\clt-tmp\cmdline-tools\" \"$d\cmdline-tools\latest\"; Remove-Item \"$d\clt-tmp\" -Recurse -Force; Remove-Item $z"
if not exist "%CLT%" (
  echo [X] Could not download the Android command-line tools. Check internet and try again.
  exit /b 1
)
:sdk_tools_ok
echo       Accepting Android SDK licences ...
(for /l %%i in (1,1,30) do @echo y) | call "%CLT%" --sdk_root="%NEWSDK%" --licenses >nul 2>&1
echo       Downloading Android 16 platform and tools ...
call "%CLT%" --sdk_root="%NEWSDK%" "platform-tools" "platforms;android-36" "build-tools;35.0.0"
if not exist "%NEWSDK%\platform-tools" (
  echo [X] Android SDK install failed.
  exit /b 1
)
set "ANDROID_HOME=%NEWSDK%"
echo       Android SDK installed.
exit /b 0
